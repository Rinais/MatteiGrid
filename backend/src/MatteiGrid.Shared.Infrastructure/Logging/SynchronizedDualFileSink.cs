using System.Globalization;
using Serilog.Core;
using Serilog.Events;

namespace MatteiGrid.Shared.Infrastructure.Logging;

/// <summary>
/// Thread-safe Serilog sink that coordinates logging across two synchronized files:
/// 1. Application log (method entry/exit, in-method warnings, errors, fatal crashes)
/// 2. Request & Response log (incoming HTTP requests and outgoing responses)
///
/// Whenever EITHER file reaches the configured line limit (e.g. 10,000 lines),
/// BOTH files roll together to the next sequence number (e.g. _001 -> _002).
/// Automatically purges log files older than the retention limit (e.g. 14 days / 2 weeks).
/// </summary>
public class SynchronizedDualFileSink : ILogEventSink, IDisposable
{
    private readonly string _logDirectory;
    private readonly int _maxLinesPerFile;
    private readonly int _retentionDays;
    private readonly object _syncLock = new();

    private string _currentDateString;
    private int _currentSequence;
    private int _appLineCount;
    private int _requestsLineCount;

    private StreamWriter? _appWriter;
    private StreamWriter? _requestsWriter;
    private bool _isDisposed;

    public SynchronizedDualFileSink(string logDirectory = "logs", int maxLinesPerFile = 10_000, int retentionDays = 14)
    {
        _logDirectory = Path.GetFullPath(logDirectory);
        _maxLinesPerFile = maxLinesPerFile > 0 ? maxLinesPerFile : 10_000;
        _retentionDays = retentionDays > 0 ? retentionDays : 14;

        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }

        var now = DateTime.Now;
        _currentDateString = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        // Find existing sequence for today if any, or start at 1
        _currentSequence = ResolveNextSequenceNumber(_currentDateString);
        OpenWriters();
        PurgeOldFiles();
    }

    public void Emit(LogEvent logEvent)
    {
        if (_isDisposed) return;

        lock (_syncLock)
        {
            var now = DateTime.Now;
            var todayStr = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            // Check if day changed
            if (!string.Equals(todayStr, _currentDateString, StringComparison.Ordinal))
            {
                RollFiles(todayStr, 1);
            }
            // Check if either file reached line limit -> trigger synchronized roll
            else if (_appLineCount >= _maxLinesPerFile || _requestsLineCount >= _maxLinesPerFile)
            {
                RollFiles(_currentDateString, _currentSequence + 1);
            }

            var formattedLine = FormatLogEvent(logEvent);

            // Route based on LogType property
            var isRequestResponse = logEvent.Properties.TryGetValue("LogType", out var logTypeVal) &&
                                    logTypeVal.ToString().Trim('"').Equals("HttpRequestResponse", StringComparison.OrdinalIgnoreCase);

            if (isRequestResponse)
            {
                _requestsWriter?.WriteLine(formattedLine);
                if (logEvent.Exception != null)
                {
                    _requestsWriter?.WriteLine(logEvent.Exception.ToString());
                }
                _requestsLineCount++;
            }
            else
            {
                _appWriter?.WriteLine(formattedLine);
                if (logEvent.Exception != null)
                {
                    _appWriter?.WriteLine(logEvent.Exception.ToString());
                }
                _appLineCount++;
            }
        }
    }

    private string FormatLogEvent(LogEvent logEvent)
    {
        var timestamp = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz");
        var level = GetFriendlyLevel(logEvent.Level);

        var sourceContext = "Application";
        if (logEvent.Properties.TryGetValue("SourceContext", out var scVal))
        {
            var raw = scVal.ToString().Trim('"');
            var lastDot = raw.LastIndexOf('.');
            sourceContext = lastDot >= 0 ? raw[(lastDot + 1)..] : raw;
        }

        var message = logEvent.RenderMessage(CultureInfo.InvariantCulture);
        return $"[{timestamp}] [{level}] [{sourceContext}] {message}";
    }

    private static string GetFriendlyLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "TRACE",
        LogEventLevel.Debug => "DEBUG",
        LogEventLevel.Information => "INFO",
        LogEventLevel.Warning => "WARN",
        LogEventLevel.Error => "ERROR",
        LogEventLevel.Fatal => "FATAL",
        _ => "INFO"
    };

    private void RollFiles(string newDateStr, int newSequence)
    {
        CloseWriters();

        _currentDateString = newDateStr;
        _currentSequence = newSequence;
        _appLineCount = 0;
        _requestsLineCount = 0;

        OpenWriters();
        PurgeOldFiles();
    }

    private void OpenWriters()
    {
        var seqStr = _currentSequence.ToString("D3", CultureInfo.InvariantCulture);
        var appPath = Path.Combine(_logDirectory, $"matteigrid-app-{_currentDateString}_{seqStr}.log");
        var requestsPath = Path.Combine(_logDirectory, $"matteigrid-requests-{_currentDateString}_{seqStr}.log");

        // If files already exist (e.g. app restart on same sequence), count lines to continue accurately
        _appLineCount = CountExistingLines(appPath);
        _requestsLineCount = CountExistingLines(requestsPath);

        _appWriter = new StreamWriter(new FileStream(appPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        {
            AutoFlush = true
        };

        _requestsWriter = new StreamWriter(new FileStream(requestsPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        {
            AutoFlush = true
        };
    }

    private static int CountExistingLines(string filePath)
    {
        if (!File.Exists(filePath)) return 0;
        try
        {
            int lines = 0;
            using var reader = new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            while (reader.ReadLine() != null)
            {
                lines++;
            }
            return lines;
        }
        catch
        {
            return 0;
        }
    }

    private int ResolveNextSequenceNumber(string dateStr)
    {
        try
        {
            var files = Directory.GetFiles(_logDirectory, $"matteigrid-*-{dateStr}_*.log");
            int maxSeq = 1;
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var parts = fileName.Split('_');
                if (parts.Length == 2 && int.TryParse(parts[1], out var seq))
                {
                    if (seq > maxSeq) maxSeq = seq;
                }
            }
            return maxSeq;
        }
        catch
        {
            return 1;
        }
    }

    private void CloseWriters()
    {
        try
        {
            _appWriter?.Flush();
            _appWriter?.Dispose();
            _appWriter = null;
        }
        catch { /* ignored */ }

        try
        {
            _requestsWriter?.Flush();
            _requestsWriter?.Dispose();
            _requestsWriter = null;
        }
        catch { /* ignored */ }
    }

    private void PurgeOldFiles()
    {
        try
        {
            var cutoff = DateTime.Now.AddDays(-_retentionDays);
            var logFiles = Directory.GetFiles(_logDirectory, "matteigrid-*.log");
            foreach (var file in logFiles)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTime < cutoff)
                {
                    fileInfo.Delete();
                }
            }
        }
        catch
        {
            // Fallback gracefully without interrupting live logging
        }
    }

    public void Dispose()
    {
        lock (_syncLock)
        {
            if (_isDisposed) return;
            _isDisposed = true;
            CloseWriters();
        }
    }
}
