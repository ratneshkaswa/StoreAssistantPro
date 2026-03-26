using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Tests.Services;

public sealed class FileLoggerProviderTests
{
    [Fact]
    public void Logger_Should_WriteStructuredContextIntoDailyFile()
    {
        var logDirectory = Path.Combine(Path.GetTempPath(), "StoreAssistantPro.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(logDirectory);

        try
        {
            using (var provider = new FileLoggerProvider(LogLevel.Debug, logDirectory))
            {
                var logger = provider.CreateLogger("Tests.FileLogger");
                logger.LogInformation("Release hardening log entry");
            }

            var logFile = Directory.GetFiles(logDirectory, "app_*.log").Single();
            var contents = File.ReadAllText(logFile);

            Assert.Contains("[Tests.FileLogger]", contents, StringComparison.Ordinal);
            Assert.Contains("Release hardening log entry", contents, StringComparison.Ordinal);
            Assert.Contains("[session=", contents, StringComparison.Ordinal);
            Assert.Contains("[machine=", contents, StringComparison.Ordinal);
            Assert.Contains("[user=", contents, StringComparison.Ordinal);
            Assert.Contains("[pid=", contents, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(logDirectory, recursive: true);
        }
    }
}
