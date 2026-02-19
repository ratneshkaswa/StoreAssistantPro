using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Resolves <see cref="ICommandHandler{TCommand}"/> from DI and
/// delegates execution. Logs every command dispatch and any failures.
/// </summary>
public class CommandBus(
    IServiceProvider serviceProvider,
    ILogger<CommandBus> logger) : ICommandBus
{
    public async Task<CommandResult> SendAsync<TCommand>(TCommand command)
        where TCommand : ICommand
    {
        var commandName = typeof(TCommand).Name;

        var handler = serviceProvider.GetService<ICommandHandler<TCommand>>()
            ?? throw new InvalidOperationException(
                $"No handler registered for command '{commandName}'.");

        logger.LogInformation("Dispatching {Command}", commandName);

        var result = await handler.HandleAsync(command);

        if (!result.Succeeded)
            logger.LogWarning("Command {Command} failed: {Error}", commandName, result.ErrorMessage);

        return result;
    }
}
