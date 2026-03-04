using System.IO;
using System.Threading.Channels;
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
/// <para>
/// <b>Non-blocking:</b> Log entries are enqueued on the caller's thread
/// and flushed to disk by a single background consumer. This prevents
/// synchronous I/O from stalling the WPF UI thread.
/// </para>
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider, IDisposable
{
    private readonly string _logDirectory;
    private readonly Dictionary<string, FileLogger> _loggers = [];
    private readonly Lock _loggerLock = new();

    private readonly Channel<string> _channel =
        Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    private readonly Task _writerTask;

    private readonly LogLevel _minimumLevel;

    public FileLoggerProvider(LogLevel minimumLevel = LogLevel.Information)
    {
        _minimumLevel = minimumLevel;
        _logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "StoreAssistantPro", "Logs");
        Directory.CreateDirectory(_logDirectory);

        _writerTask = Task.Run(ProcessQueueAsync);
    }

    public ILogger CreateLogger(string categoryName)
    {
        lock (_loggerLock)
        {
            if (!_loggers.TryGetValue(categoryName, out var logger))
            {
                logger = new FileLogger(categoryName, this, _minimumLevel);
                _loggers[categoryName] = logger;
            }
            return logger;
        }
    }

    public void Dispose()
    {
        _channel.Writer.TryComplete();

        // Use a timeout to prevent deadlock if Dispose is called from
        // the UI thread while the writer task is blocked.
        if (!_writerTask.Wait(TimeSpan.FromSeconds(5)))
        {
            // Best-effort — some log entries may be lost on abrupt shutdown.
        }
    }

    internal void EnqueueEntry(string categoryName, LogLevel level, string message, Exception? exception)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var entry = $"[{timestamp}Z] [{level,-11}] [{categoryName}] {message}";

        if (exception is not null)
            entry += $"{Environment.NewLine}  Exception: {exception}";

        _channel.Writer.TryWrite(entry);
    }

    private async Task ProcessQueueAsync()
    {
        var reader = _channel.Reader;
        while (await reader.WaitToReadAsync().ConfigureAwait(false))
        {
            var logFile = Path.Combine(_logDirectory, $"app_{DateTime.UtcNow:yyyyMMdd}.log");

            await using var writer = new StreamWriter(logFile, append: true);
            while (reader.TryRead(out var entry))
            {
                await writer.WriteLineAsync(entry).ConfigureAwait(false);
            }
            // Flush is implicit via await using (Dispose flushes),
            // and closing the handle after each batch ensures durability.
        }
    }
}

internal sealed class FileLogger(string categoryName, FileLoggerProvider provider, LogLevel minimumLevel) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= minimumLevel;

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        provider.EnqueueEntry(categoryName, logLevel, formatter(state, exception), exception);
    }
}
