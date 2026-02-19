using Microsoft.Extensions.DependencyInjection;

namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Resolves <see cref="ICommandHandler{TCommand}"/> from DI and
/// delegates execution. One line of indirection — zero magic.
/// </summary>
public class CommandBus(IServiceProvider serviceProvider) : ICommandBus
{
    public async Task<CommandResult> SendAsync<TCommand>(TCommand command)
        where TCommand : ICommand
    {
        var handler = serviceProvider.GetService<ICommandHandler<TCommand>>()
            ?? throw new InvalidOperationException(
                $"No handler registered for command '{typeof(TCommand).Name}'.");

        return await handler.HandleAsync(command);
    }
}
