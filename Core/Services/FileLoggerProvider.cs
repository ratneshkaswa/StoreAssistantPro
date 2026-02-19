using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Logging;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Lightweight file logger that appends structured entries to a daily
/// log file. No third-party dependencies — uses the built-in
/// <see cref="ILoggerProvider"/> contract.
/// <para>
/// Log files are written to <c>Documents/StoreAssistantPro/Logs/</c>
/// with a daily rotation pattern: <c>app_20260219.log</c>.
/// </para>
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logDirectory;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private readonly Lock _writeLock = new();

    public FileLoggerProvider()
    {
        _logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "StoreAssistantPro", "Logs");
        Directory.CreateDirectory(_logDirectory);
    }

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new FileLogger(name, this));

    public void Dispose() => _loggers.Clear();

    internal void WriteEntry(string categoryName, LogLevel level, string message, Exception? exception)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logFile = Path.Combine(_logDirectory, $"app_{DateTime.Now:yyyyMMdd}.log");
        var entry = $"[{timestamp}] [{level,-11}] [{categoryName}] {message}";

        if (exception is not null)
            entry += $"{Environment.NewLine}  Exception: {exception}";

        lock (_writeLock)
        {
            File.AppendAllText(logFile, entry + Environment.NewLine);
        }
    }
}

internal sealed class FileLogger(string categoryName, FileLoggerProvider provider) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        provider.WriteEntry(categoryName, logLevel, formatter(state, exception), exception);
    }
}
