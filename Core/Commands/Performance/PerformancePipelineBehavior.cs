using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Commands.Performance;

/// <summary>
/// Pipeline behavior that measures command execution time using
/// <see cref="IPerformanceMonitor"/> and logs slow commands via
/// the centralized performance monitoring infrastructure.
/// <para>
/// <b>Difference from <see cref="Logging.LoggingPipelineBehavior{TCommand,TResult}"/>:</b>
/// <list type="bullet">
///   <item><b>LoggingBehavior</b> — logs command start/outcome/duration
///         via <see cref="ILogger"/>. Sits early in the pipeline to
///         measure total wall-clock time including all behaviors.</item>
///   <item><b>PerformanceBehavior</b> (this) — records timing through
///         <see cref="IPerformanceMonitor"/> for centralized metrics.
///         Sits closest to the handler to measure the <b>inner
///         execution time</b> (handler + transaction) without
///         validation or logging overhead. Warns on slow commands
///         at <see cref="LogLevel.Warning"/> via the monitor.</item>
/// </list>
/// </para>
/// <para>
/// <b>Pipeline position:</b> Register as the <b>last</b> behavior
/// (innermost wrapper around the handler) so the measurement
/// captures the true handler execution cost:
/// <code>
/// services.AddTransient(typeof(ICommandPipelineBehavior&lt;,&gt;),
///     typeof(ValidationPipelineBehavior&lt;,&gt;));
/// services.AddTransient(typeof(ICommandPipelineBehavior&lt;,&gt;),
///     typeof(LoggingPipelineBehavior&lt;,&gt;));
/// services.AddTransient(typeof(ICommandPipelineBehavior&lt;,&gt;),
///     typeof(OfflinePipelineBehavior&lt;,&gt;));
/// services.AddTransient(typeof(ICommandPipelineBehavior&lt;,&gt;),
///     typeof(TransactionPipelineBehavior&lt;,&gt;));
/// services.AddTransient(typeof(ICommandPipelineBehavior&lt;,&gt;),
///     typeof(PerformancePipelineBehavior&lt;,&gt;));   // ← innermost
/// </code>
/// </para>
/// <para>
/// <b>Integration:</b> Uses the same <see cref="IPerformanceMonitor"/>
/// / <see cref="TimedScope"/> pattern as
/// <see cref="TransactionSafetyService"/> and other infrastructure
/// services. The <see cref="TimedScope"/> automatically logs at
/// <see cref="LogLevel.Debug"/> for fast commands and
/// <see cref="LogLevel.Warning"/> for slow commands (threshold
/// defaults to 500 ms, matching the monitor's default).
/// </para>
/// </summary>
public sealed class PerformancePipelineBehavior<TCommand, TResult>(
    IPerformanceMonitor performanceMonitor,
    ILogger<PerformancePipelineBehavior<TCommand, TResult>> logger)
    : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommandRequest<TResult>
{
    private static readonly string CommandName = typeof(TCommand).Name;
    private static readonly string ScopeName = $"Command.{CommandName}";

    public async Task<CommandResult<TResult>> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResult> next,
        CancellationToken ct = default)
    {
        using var scope = performanceMonitor.BeginScope(ScopeName);

        var result = await next().ConfigureAwait(false);

        if (!result.Succeeded)
        {
            logger.LogDebug(
                "Command {Command} completed with failure: {Error}",
                CommandName, result.ErrorMessage);
        }

        return result;
    }
}
