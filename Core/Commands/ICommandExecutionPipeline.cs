namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Executes an <see cref="ICommandRequest{TResult}"/> through the
/// full pipeline: all registered
/// <see cref="ICommandPipelineBehavior{TCommand,TResult}"/> behaviors
/// in DI registration order, then the inner
/// <see cref="ICommandRequestHandler{TCommand,TResult}"/>.
/// <para>
/// <b>Pipeline flow:</b>
/// <code>
/// ExecuteAsync(command, ct)
///   │
///   ▼  Behavior 1 (outermost — first registered)
///   ├─ before logic
///   ├─ await next()
///   │     ▼  Behavior 2
///   │     ├─ before logic
///   │     ├─ await next()
///   │     │     ▼  Handler (innermost)
///   │     │     └─ CommandResult&lt;TResult&gt;
///   │     └─ after logic
///   └─ after logic
///
///   ▼  CommandResult&lt;TResult&gt; returned to caller
/// </code>
/// </para>
/// <para>
/// <b>Zero behaviors:</b> If no behaviors are registered for a
/// command type, the handler is invoked directly — no overhead.
/// </para>
/// <para>
/// Registered as a <b>singleton</b> in DI. Behaviors and handlers
/// are resolved per-call from a scoped or transient lifetime.
/// </para>
/// </summary>
public interface ICommandExecutionPipeline
{
    /// <summary>
    /// Executes the command through all registered pipeline behaviors
    /// and the inner handler.
    /// </summary>
    /// <typeparam name="TCommand">
    /// The command request type. Must implement
    /// <see cref="ICommandRequest{TResult}"/>.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The typed result produced by the handler.
    /// </typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="ct">Cancellation token forwarded to all steps.</param>
    /// <returns>The typed result from the pipeline.</returns>
    Task<CommandResult<TResult>> ExecuteAsync<TCommand, TResult>(
        TCommand command, CancellationToken ct = default)
        where TCommand : ICommandRequest<TResult>;
}
