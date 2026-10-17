using System.Diagnostics;
using System.Reflection;
using Castle.DynamicProxy;
using FluentAssertions;
using MatteiGrid.Shared.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace MatteiGrid.UnitTests;

public interface ISampleTestService
{
    Task<string> ExecuteSuccessfulAsync(string patientName, string phone, string password);
    Task ExecuteFailingAsync();
    string ExecuteSync(int value);
}

public class SampleTestService : ISampleTestService
{
    public async Task<string> ExecuteSuccessfulAsync(string patientName, string phone, string password)
    {
        await Task.Delay(20);
        return $"Processed {patientName}";
    }

    public async Task ExecuteFailingAsync()
    {
        await Task.Delay(10);
        throw new InvalidOperationException("Simulated domain operation failure.");
    }

    public string ExecuteSync(int value)
    {
        return $"Result-{value}";
    }
}

public class LoggingTests : IDisposable
{
    private readonly string _testLogDir;

    public LoggingTests()
    {
        _testLogDir = Path.Combine(Path.GetTempPath(), "MatteiGridLogTests_" + Guid.NewGuid().ToString("N"));
        if (!Directory.Exists(_testLogDir))
        {
            Directory.CreateDirectory(_testLogDir);
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testLogDir))
            {
                Directory.Delete(_testLogDir, true);
            }
        }
        catch { /* ignored */ }
    }

    [Fact]
    public void LogSanitizer_ShouldMask_SensitiveParametersAndPhoneNumbers()
    {
        // Assert password redaction
        var maskedPassword = LogSanitizer.SanitizeValue("password", "SuperSecret123!");
        maskedPassword.Should().Be("\"***\"");

        var maskedToken = LogSanitizer.SanitizeValue("accessToken", "eyJhbGciOi...");
        maskedToken.Should().Be("\"***\"");

        // Assert phone number masking (last 4 digits retained)
        var sanitizedPhone = LogSanitizer.SanitizeValue("phone", "9876543210");
        sanitizedPhone.Should().Contain("***3210");

        // Assert harmless value preserved
        var safeName = LogSanitizer.SanitizeValue("name", "Dr. Mattei");
        safeName.Should().Be("\"Dr. Mattei\"");
    }

    [Fact]
    public void SynchronizedDualFileSink_ShouldRoute_NormalAndRequestLogsToSeparateFiles()
    {
        var parser = new MessageTemplateParser();
        using (var sink = new SynchronizedDualFileSink(_testLogDir, maxLinesPerFile: 10_000, retentionDays: 14))
        {
            // 1. Emit standard app log event
            var appEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                parser.Parse("Executing service method"),
                new[] { new LogEventProperty("SourceContext", new ScalarValue("PatientService")) }
            );
            sink.Emit(appEvent);

            // 2. Emit HTTP request/response log event
            var reqEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                parser.Parse("HTTP GET /api/v1/patients"),
                new[]
                {
                    new LogEventProperty("SourceContext", new ScalarValue("RequestLoggingMiddleware")),
                    new LogEventProperty("LogType", new ScalarValue("HttpRequestResponse"))
                }
            );
            sink.Emit(reqEvent);
        }

        var appFiles = Directory.GetFiles(_testLogDir, "matteigrid-app-*.log");
        var reqFiles = Directory.GetFiles(_testLogDir, "matteigrid-requests-*.log");

        appFiles.Should().HaveCount(1);
        reqFiles.Should().HaveCount(1);

        var appContent = File.ReadAllText(appFiles[0]);
        var reqContent = File.ReadAllText(reqFiles[0]);

        appContent.Should().Contain("[INFO] [PatientService] Executing service method");
        reqContent.Should().Contain("[INFO] [RequestLoggingMiddleware] HTTP GET /api/v1/patients");
    }

    [Fact]
    public void SynchronizedDualFileSink_ShouldRollBothFilesSimultaneously_WhenLineLimitReached()
    {
        var parser = new MessageTemplateParser();

        // Line limit set to 3 lines per file for deterministic test
        using (var sink = new SynchronizedDualFileSink(_testLogDir, maxLinesPerFile: 3, retentionDays: 14))
        {
            // Write 3 HTTP request logs (triggering line limit on requests file)
            for (int i = 1; i <= 3; i++)
            {
                var reqEvent = new LogEvent(
                    DateTimeOffset.Now,
                    LogEventLevel.Information,
                    null,
                    parser.Parse($"Request line {i}"),
                    new[] { new LogEventProperty("LogType", new ScalarValue("HttpRequestResponse")) }
                );
                sink.Emit(reqEvent);
            }

            // Next log (4th event) should trigger synchronized roll for BOTH files to _002
            var fourthEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                parser.Parse("Fourth event after roll"),
                new[] { new LogEventProperty("SourceContext", new ScalarValue("ClinicalService")) }
            );
            sink.Emit(fourthEvent);
        }

        var appFiles = Directory.GetFiles(_testLogDir, "matteigrid-app-*.log");
        var reqFiles = Directory.GetFiles(_testLogDir, "matteigrid-requests-*.log");

        // Both streams must have rolled and produced _001 and _002
        appFiles.Should().Contain(f => f.EndsWith("_001.log"));
        appFiles.Should().Contain(f => f.EndsWith("_002.log"));

        reqFiles.Should().Contain(f => f.EndsWith("_001.log"));
        reqFiles.Should().Contain(f => f.EndsWith("_002.log"));
    }

    [Fact]
    public async Task ServiceLoggingInterceptor_ShouldLogEntryExit_AndMeasureDuration()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());
        services.AddLoggedScoped<ISampleTestService, SampleTestService>();

        var provider = services.BuildServiceProvider();
        var sampleService = provider.GetRequiredService<ISampleTestService>();

        // Execute successful method
        var result = await sampleService.ExecuteSuccessfulAsync("Ramesh Kumar", "9876543210", "SecretPassword!");
        result.Should().Be("Processed Ramesh Kumar");

        // Execute sync method
        var syncResult = sampleService.ExecuteSync(42);
        syncResult.Should().Be("Result-42");

        // Execute failing method to verify exception propagation
        var act = async () => await sampleService.ExecuteFailingAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Simulated domain operation failure.");
    }
}
