using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Core;

/// <summary>
/// Base class for all command handlers. Provides a consistent
/// try/catch → <see cref="CommandResult"/> pattern so handlers
/// only contain business logic.
/// <para>
/// Implements both <see cref="ICommandHandler{TCommand}"/> (legacy
/// contract kept for direct-call backward compatibility) and
/// <see cref="ICommandRequestHandler{TCommand,TResult}"/> with
/// <see cref="Unit"/> so the handler participates in the enterprise
/// pipeline (behaviors → handler).
/// </para>
/// <para>Usage:</para>
/// <code>
/// public class SaveProductHandler(IProductService svc)
///     : BaseCommandHandler&lt;SaveProductCommand&gt;
/// {
///     protected override async Task&lt;CommandResult&gt; ExecuteAsync(SaveProductCommand cmd)
///     {
///         await svc.AddAsync(...);
///         return CommandResult.Success();
///     }
/// }
/// </code>
/// </summary>
public abstract class BaseCommandHandler<TCommand>
    : ICommandHandler<TCommand>,
      ICommandRequestHandler<TCommand, Unit>
    where TCommand : ICommandRequest<Unit>
{
    /// <summary>
    /// Template method: wraps <see cref="ExecuteAsync"/> in a
    /// try/catch and converts unhandled exceptions to
    /// <see cref="CommandResult.Failure"/>.
    /// </summary>
    public async Task<CommandResult> HandleAsync(TCommand command)
    {
        try
        {
            return await ExecuteAsync(command, CancellationToken.None);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Pipeline-compatible entry point. Delegates to
    /// <see cref="ExecuteAsync(TCommand, CancellationToken)"/> and
    /// wraps the untyped <see cref="CommandResult"/> into a
    /// <see cref="CommandResult{Unit}"/>.
    /// <para>
    /// <b>CancellationToken forwarding:</b> The pipeline receives
    /// <paramref name="ct"/> from the bus and passes it through to the
    /// handler so that EF Core calls, HTTP requests, and other async
    /// work are properly cancellable.
    /// </para>
    /// </summary>
    async Task<CommandResult<Unit>> ICommandRequestHandler<TCommand, Unit>.HandleAsync(
        TCommand command, CancellationToken ct)
    {
        CommandResult result;
        try
        {
            result = await ExecuteAsync(command, ct);
        }
        catch (Exception ex)
        {
            result = CommandResult.Failure(ex.Message);
        }

        return result.Succeeded
            ? CommandResult<Unit>.Success(Unit.Value)
            : CommandResult<Unit>.Failure(result.ErrorMessage ?? "Unknown error");
    }

    /// <summary>
    /// Implement the business logic for the command. Return
    /// <see cref="CommandResult.Success"/> or
    /// <see cref="CommandResult.Failure"/> for expected outcomes.
    /// Unexpected exceptions are caught by the base class.
    /// <para>
    /// Override this method (with <paramref name="ct"/>) to receive
    /// the <see cref="CancellationToken"/> from the command pipeline.
    /// The default implementation delegates to the parameterless
    /// overload for backward compatibility.
    /// </para>
    /// </summary>
    protected virtual Task<CommandResult> ExecuteAsync(TCommand command, CancellationToken ct)
        => ExecuteAsync(command);

    /// <summary>
    /// Parameterless overload kept for backward compatibility.
    /// New handlers should override the <c>(TCommand, CancellationToken)</c>
    /// overload instead to receive pipeline cancellation.
    /// </summary>
    protected virtual Task<CommandResult> ExecuteAsync(TCommand command)
        => throw new NotImplementedException(
            $"Handler {GetType().Name} must override ExecuteAsync(TCommand) or ExecuteAsync(TCommand, CancellationToken).");
}
