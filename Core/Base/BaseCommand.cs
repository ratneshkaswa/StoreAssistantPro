using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Core;

/// <summary>
/// Base class for all command handlers. Provides a consistent
/// try/catch → <see cref="CommandResult"/> pattern so handlers
/// only contain business logic.
/// <para>
/// <b>Architecture rule:</b> Every <see cref="ICommandHandler{TCommand}"/>
/// must inherit from <see cref="BaseCommandHandler{TCommand}"/> to
/// guarantee uniform error handling across all modules.
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
public abstract class BaseCommandHandler<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
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
            return await ExecuteAsync(command);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Implement the business logic for the command. Return
    /// <see cref="CommandResult.Success"/> or
    /// <see cref="CommandResult.Failure"/> for expected outcomes.
    /// Unexpected exceptions are caught by the base class.
    /// </summary>
    protected abstract Task<CommandResult> ExecuteAsync(TCommand command);
}
