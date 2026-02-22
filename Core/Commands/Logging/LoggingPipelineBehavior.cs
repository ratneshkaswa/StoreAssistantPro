using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace StoreAssistantPro.Core.Commands.Logging;

/// <summary>
/// Pipeline behavior that logs the start, outcome, and duration of
/// every command that flows through the enterprise pipeline.
/// <para>
/// <b>Log levels:</b>
/// <list type="bullet">
///   <item><see cref="LogLevel.Information"/> — command start and
///         successful completion (with elapsed time).</item>
///   <item><see cref="LogLevel.Warning"/> — command failure (with
///         error message and elapsed time).</item>
///   <item><see cref="LogLevel.Warning"/> — slow command (elapsed
///         exceeds <see cref="SlowCommandThreshold"/>).</item>
/// </list>
/// </para>
/// <para>
/// <b>Pipeline position:</b> Register <b>after</b>
/// <see cref="Validation.ValidationPipelineBehavior{TCommand,TResult}"/>
/// so that validation failures are logged as normal failures, but
/// before other behaviors so the duration measurement covers the
/// entire inner pipeline:
/// <code>
/// services.AddTransient(typeof(ICommandPipelineBehavior&lt;,&gt;),
///     typeof(ValidationPipelineBehavior&lt;,&gt;));
/// services.AddTransient(typeof(ICommandPipelineBehavior&lt;,&gt;),
///     typeof(LoggingPipelineBehavior&lt;,&gt;));
/// </code>
/// </para>
/// <para>
/// <b>Timing:</b> Uses <see cref="Stopwatch.GetTimestamp"/> /
/// <see cref="Stopwatch.GetElapsedTime"/> for high-resolution,
/// allocation-free measurement — matching the pattern used by
/// <see cref="Services.PerformanceMonitor"/>.
/// </para>
/// </summary>
public sealed class LoggingPipelineBehavior<TCommand, TResult>(
    ILogger<LoggingPipelineBehavior<TCommand, TResult>> logger)
    : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommandRequest<TResult>
{
    /// <summary>
    /// Commands taking longer than this threshold trigger an
    /// additional <see cref="LogLevel.Warning"/>.
    /// </summary>
    public static readonly TimeSpan SlowCommandThreshold = TimeSpan.FromMilliseconds(500);

    private static readonly string CommandName = typeof(TCommand).Name;

    public async Task<CommandResult<TResult>> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResult> next,
        CancellationToken ct = default)
    {
        logger.LogInformation("Executing {Command}", CommandName);

        var startTimestamp = Stopwatch.GetTimestamp();

        var result = await next().ConfigureAwait(false);

        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

        if (result.Succeeded)
        {
            logger.LogInformation(
                "Command {Command} succeeded in {ElapsedMs:F1} ms",
                CommandName, elapsed.TotalMilliseconds);
        }
        else
        {
            logger.LogWarning(
                "Command {Command} failed in {ElapsedMs:F1} ms: {Error}",
                CommandName, elapsed.TotalMilliseconds, result.ErrorMessage);
        }

        if (elapsed > SlowCommandThreshold)
        {
            logger.LogWarning(
                "Slow command detected: {Command} took {ElapsedMs:F1} ms (threshold: {ThresholdMs} ms)",
                CommandName, elapsed.TotalMilliseconds, SlowCommandThreshold.TotalMilliseconds);
        }

        return result;
    }
}
