namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Dispatches commands to their registered handlers via DI.
/// ViewModels call <see cref="SendAsync{TCommand}"/> instead of
/// calling services directly.
/// </summary>
public interface ICommandBus
{
    Task<CommandResult> SendAsync<TCommand>(TCommand command) where TCommand : ICommand;
}
