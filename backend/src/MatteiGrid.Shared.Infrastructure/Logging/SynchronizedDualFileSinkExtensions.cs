using Serilog;
using Serilog.Configuration;

namespace MatteiGrid.Shared.Infrastructure.Logging;

public static class SynchronizedDualFileSinkExtensions
{
    public static LoggerConfiguration SynchronizedDualFile(
        this LoggerSinkConfiguration sinkConfiguration,
        string logDirectory = "logs",
        int maxLinesPerFile = 10_000,
        int retentionDays = 14)
    {
        return sinkConfiguration.Sink(new SynchronizedDualFileSink(logDirectory, maxLinesPerFile, retentionDays));
    }
}
