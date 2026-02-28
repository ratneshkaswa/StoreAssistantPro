namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Handles a specific <see cref="ICommandRequest{TResult}"/> and
/// returns a typed <see cref="CommandResult{TResult}"/>.
/// <para>
/// <b>Relationship to <see cref="ICommandHandler{TCommand}"/>:</b>
/// <list type="bullet">
///   <item><see cref="ICommandHandler{TCommand}"/> — existing handler
///         for simple <see cref="ICommand"/> commands. Returns
///         untyped <see cref="CommandResult"/>.</item>
///   <item><see cref="ICommandRequestHandler{TCommand,TResult}"/> —
///         enterprise handler for typed requests. Returns
///         <see cref="CommandResult{TResult}"/> and participates
///         in the pipeline.</item>
/// </list>
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// public class CreateOrderHandler(IOrderService orders)
///     : ICommandRequestHandler&lt;CreateOrderCommand, int&gt;
/// {
///     public async Task&lt;CommandResult&lt;int&gt;&gt; HandleAsync(
///         CreateOrderCommand command, CancellationToken ct)
///     {
///         var id = await orders.CreateAsync(command, ct);
///         return CommandResult&lt;int&gt;.Success(id);
///     }
/// }
/// </code>
/// </para>
/// <para>
/// Registered in DI as:
/// <code>
/// services.AddTransient&lt;ICommandRequestHandler&lt;CreateOrderCommand, int&gt;,
///                        CreateOrderHandler&gt;();
/// </code>
/// </para>
/// </summary>
/// <typeparam name="TCommand">
/// The command request type. Must implement
/// <see cref="ICommandRequest{TResult}"/>.
/// </typeparam>
/// <typeparam name="TResult">
/// The type of value produced on successful execution.
/// </typeparam>
public interface ICommandRequestHandler<in TCommand, TResult>
    where TCommand : ICommandRequest<TResult>
{
    /// <summary>
    /// Executes the command and returns a typed result.
    /// </summary>
    /// <param name="command">The command request to handle.</param>
    /// <param name="ct">
    /// Cancellation token forwarded from the bus. Handlers should
    /// pass this to all async service calls.
    /// </param>
    Task<CommandResult<TResult>> HandleAsync(TCommand command, CancellationToken ct = default);
}
