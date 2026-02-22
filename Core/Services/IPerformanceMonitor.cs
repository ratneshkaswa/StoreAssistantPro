using Microsoft.Extensions.Logging;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Lightweight performance measurement service. Inject into any service
/// to time critical operations and automatically log slow ones.
/// <para>
/// <b>Usage:</b>
/// <code>
/// using (perf.BeginScope("ProductService.GetPagedAsync"))
/// {
///     // … operation …
/// }
/// // If elapsed > threshold → Warning logged automatically.
/// </code>
/// </para>
/// </summary>
public interface IPerformanceMonitor
{
    /// <summary>
    /// Starts timing an operation. The returned scope logs a warning
    /// via <see cref="ILogger"/> if elapsed time exceeds
    /// <paramref name="slowThreshold"/> (default 500 ms).
    /// </summary>
    /// <param name="operationName">
    /// Human-readable name for the operation (e.g. "LoginService.ValidatePinAsync").
    /// </param>
    /// <param name="slowThreshold">
    /// Optional override for the slow-operation threshold.
    /// Operations completing within this duration are logged at Debug level.
    /// </param>
    TimedScope BeginScope(string operationName, TimeSpan? slowThreshold = null);
}
