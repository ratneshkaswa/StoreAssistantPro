using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Singleton performance monitor. Each <see cref="BeginScope"/> call
/// returns a <see cref="TimedScope"/> that measures wall-clock elapsed
/// time and logs the result on dispose.
/// <list type="bullet">
///   <item><b>Fast path (≤ threshold):</b> logged at <see cref="LogLevel.Debug"/>.</item>
///   <item><b>Slow path (&gt; threshold):</b> logged at <see cref="LogLevel.Warning"/>.</item>
/// </list>
/// </summary>
public sealed class PerformanceMonitor(ILogger<PerformanceMonitor> logger) : IPerformanceMonitor
{
    private static readonly TimeSpan DefaultThreshold = TimeSpan.FromMilliseconds(500);

    public TimedScope BeginScope(string operationName, TimeSpan? slowThreshold = null) =>
        new(operationName, slowThreshold ?? DefaultThreshold, logger);
}

/// <summary>
/// Disposable scope that measures elapsed time between creation and
/// dispose. Designed as a <c>class</c> (not <c>ref struct</c>) so it
/// works with <c>using</c> declarations and <c>await using</c> inside
/// async methods — the scope may cross an <c>await</c> boundary.
/// <para>
/// Allocation cost is negligible for service-level operations that
/// already allocate DbContexts, lists, and EF queries.
/// </para>
/// </summary>
public sealed class TimedScope : IDisposable
{
    private readonly string _operationName;
    private readonly TimeSpan _slowThreshold;
    private readonly ILogger _logger;
    private readonly long _startTimestamp;

    internal TimedScope(string operationName, TimeSpan slowThreshold, ILogger logger)
    {
        _operationName = operationName;
        _slowThreshold = slowThreshold;
        _logger = logger;
        _startTimestamp = Stopwatch.GetTimestamp();
    }

    public void Dispose()
    {
        var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);

        if (elapsed > _slowThreshold)
        {
            _logger.LogWarning(
                "Slow operation: {Operation} completed in {ElapsedMs:F1} ms (threshold: {ThresholdMs} ms)",
                _operationName, elapsed.TotalMilliseconds, _slowThreshold.TotalMilliseconds);
        }
        else
        {
            _logger.LogDebug(
                "{Operation} completed in {ElapsedMs:F1} ms",
                _operationName, elapsed.TotalMilliseconds);
        }
    }
}
