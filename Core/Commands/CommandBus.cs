using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Resolves handlers from DI and delegates execution.
/// <para>
/// <b>Simple commands</b> (<see cref="ICommand"/>) are dispatched
/// directly to <see cref="ICommandHandler{TCommand}"/>.
/// </para>
/// <para>
/// <b>Enterprise commands</b> (<see cref="ICommandRequest{TResult}"/>)
/// are dispatched through <see cref="ICommandExecutionPipeline"/>
/// which chains all registered
/// <see cref="ICommandPipelineBehavior{TCommand,TResult}"/> behaviors
/// around the inner handler.
/// </para>
/// </summary>
public class CommandBus(
    IServiceProvider serviceProvider,
    ICommandExecutionPipeline pipeline,
    ILogger<CommandBus> logger) : ICommandBus
{
    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public Task<CommandResult<TResult>> SendAsync<TCommand, TResult>(
        TCommand command, CancellationToken ct = default)
        where TCommand : ICommandRequest<TResult>
    {
        return pipeline.ExecuteAsync<TCommand, TResult>(command, ct);
    }
}
