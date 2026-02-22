namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Middleware that wraps command execution in the pipeline. Behaviors
/// are resolved from DI and executed in registration order, forming a
/// Russian-doll chain around the inner handler.
/// <para>
/// <b>Pipeline flow:</b>
/// <code>
/// CommandBus.SendAsync&lt;TCommand, TResult&gt;(command)
///   │
///   ▼  Behavior 1 (outermost)
///   ├─ before logic (validation, logging, timing…)
///   ├─ await next()          ─── calls Behavior 2
///   │                              ├─ await next()  ─── calls Handler
///   │                              │                       └─ returns result
///   │                              └─ after logic
///   └─ after logic (audit, metrics…)
///
///   ▼  CommandResult&lt;TResult&gt; returned to caller
/// </code>
/// </para>
/// <para>
/// <b>Design rules:</b>
/// <list type="bullet">
///   <item>Behaviors are cross-cutting concerns — they must not
///         contain business logic specific to one command.</item>
///   <item>Behaviors <b>must</b> call <paramref name="next"/> exactly
///         once (or return early for short-circuit scenarios like
///         validation failure).</item>
///   <item>Behaviors are registered as open generics in DI:
///         <c>services.AddTransient(typeof(ICommandPipelineBehavior&lt;,&gt;),
///         typeof(LoggingBehavior&lt;,&gt;));</c></item>
///   <item>Order of execution matches DI registration order.</item>
/// </list>
/// </para>
/// <para>
/// <b>Common behaviors:</b>
/// <list type="bullet">
///   <item><b>Logging</b> — log command name, duration, success/failure.</item>
///   <item><b>Validation</b> — validate command properties before
///         the handler runs; return <see cref="CommandResult{TResult}.Failure"/>
///         without calling <paramref name="next"/>.</item>
///   <item><b>Performance</b> — measure execution time and warn on
///         slow commands.</item>
///   <item><b>Exception handling</b> — catch unhandled exceptions and
///         convert to <see cref="CommandResult{TResult}.Failure"/>.</item>
///   <item><b>Offline guard</b> — reject DB-dependent commands when
///         the app is offline.</item>
/// </list>
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// public class LoggingBehavior&lt;TCommand, TResult&gt;(ILogger logger)
///     : ICommandPipelineBehavior&lt;TCommand, TResult&gt;
///     where TCommand : ICommandRequest&lt;TResult&gt;
/// {
///     public async Task&lt;CommandResult&lt;TResult&gt;&gt; HandleAsync(
///         TCommand command,
///         CommandHandlerDelegate&lt;TResult&gt; next,
///         CancellationToken ct)
///     {
///         logger.LogInformation("Executing {Command}", typeof(TCommand).Name);
///         var result = await next();
///         logger.LogInformation("Completed {Command}: {Succeeded}",
///             typeof(TCommand).Name, result.Succeeded);
///         return result;
///     }
/// }
/// </code>
/// </para>
/// </summary>
/// <typeparam name="TCommand">
/// The command request type flowing through the pipeline.
/// </typeparam>
/// <typeparam name="TResult">
/// The result type produced by the handler at the end of the pipeline.
/// </typeparam>
public interface ICommandPipelineBehavior<in TCommand, TResult>
    where TCommand : ICommandRequest<TResult>
{
    /// <summary>
    /// Executes this behavior's logic around the next step in the
    /// pipeline.
    /// </summary>
    /// <param name="command">The command request being processed.</param>
    /// <param name="next">
    /// Delegate that invokes the next behavior in the chain, or the
    /// inner handler if this is the last behavior. Must be called
    /// exactly once (or not at all for short-circuit).
    /// </param>
    /// <param name="ct">Cancellation token from the caller.</param>
    /// <returns>
    /// The result from the inner pipeline, optionally modified by
    /// this behavior.
    /// </returns>
    Task<CommandResult<TResult>> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResult> next,
        CancellationToken ct = default);
}

/// <summary>
/// Delegate representing the next step in the command pipeline.
/// Called by <see cref="ICommandPipelineBehavior{TCommand,TResult}"/>
/// to continue execution.
/// </summary>
/// <typeparam name="TResult">Result type of the command.</typeparam>
/// <returns>The result from the next behavior or the inner handler.</returns>
public delegate Task<CommandResult<TResult>> CommandHandlerDelegate<TResult>();
