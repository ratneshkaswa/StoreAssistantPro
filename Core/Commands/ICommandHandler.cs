namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Handles a specific <typeparamref name="TCommand"/>. Each handler
/// encapsulates one business action — call services, update state,
/// persist data — then returns a <see cref="CommandResult"/>.
/// </summary>
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task<CommandResult> HandleAsync(TCommand command);
}
