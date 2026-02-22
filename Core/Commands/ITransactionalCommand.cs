namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Marker interface that opts a command into automatic transaction
/// wrapping by
/// <see cref="Transaction.TransactionPipelineBehavior{TCommand,TResult}"/>.
/// <para>
/// <b>When to use:</b> Apply to any
/// <see cref="ICommandRequest{TResult}"/> that performs database writes
/// (inserts, updates, deletes). The pipeline behavior will wrap the
/// entire inner pipeline execution in
/// <see cref="Services.ITransactionSafetyService.ExecuteSafeAsync{TResult}"/>
/// — commit on success, rollback on failure.
/// </para>
/// <para>
/// <b>When NOT to use:</b>
/// <list type="bullet">
///   <item>Read-only queries — no transaction needed.</item>
///   <item>Commands that only publish events or update in-memory
///         state — no DB writes to protect.</item>
///   <item>Commands that manage their own transaction internally
///         (to avoid nested transactions).</item>
/// </list>
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// public sealed record CreateOrderCommand(
///     Guid IdempotencyKey,
///     IReadOnlyList&lt;OrderLineDto&gt; Lines)
///     : ICommandRequest&lt;int&gt;, ITransactionalCommand;
/// </code>
/// </para>
/// </summary>
public interface ITransactionalCommand;
