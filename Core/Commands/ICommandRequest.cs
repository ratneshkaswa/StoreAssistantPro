namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Marker interface for enterprise command requests. Extends the basic
/// <see cref="ICommand"/> contract with a typed result and request
/// metadata support.
/// <para>
/// <b>Relationship to <see cref="ICommand"/>:</b>
/// <list type="bullet">
///   <item><see cref="ICommand"/> — simple fire-and-forget commands
///         that return <see cref="CommandResult"/>. Used by existing
///         handlers and the current <see cref="ICommandBus"/>.</item>
///   <item><see cref="ICommandRequest{TResult}"/> — enterprise commands
///         that carry a typed result and participate in the pipeline.
///         Used by <see cref="ICommandPipelineBehavior{TCommand,TResult}"/>
///         and the pipeline-aware bus.</item>
/// </list>
/// </para>
/// <para>
/// <b>Design rules:</b>
/// <list type="bullet">
///   <item>Commands are immutable DTOs — use <c>sealed record</c>.</item>
///   <item>Commands carry all data needed to execute the action.</item>
///   <item><typeparamref name="TResult"/> describes the success value;
///         failures are conveyed via <see cref="CommandResult"/>.</item>
///   <item>Commands do not contain business logic or service
///         references.</item>
/// </list>
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// public sealed record CreateOrderCommand(
///     Guid IdempotencyKey,
///     IReadOnlyList&lt;OrderLineDto&gt; Lines)
///     : ICommandRequest&lt;int&gt;;   // returns Order.Id on success
/// </code>
/// </para>
/// </summary>
/// <typeparam name="TResult">
/// The type of value produced on successful execution. Wrapped in
/// <see cref="CommandResult{TResult}"/> by the handler.
/// </typeparam>
public interface ICommandRequest<TResult> : ICommand;
