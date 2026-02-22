using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Builds and executes the Russian-doll pipeline for
/// <see cref="ICommandRequest{TResult}"/> commands.
/// <para>
/// <b>Resolution:</b> On each call the pipeline resolves:
/// <list type="number">
///   <item>All <see cref="ICommandPipelineBehavior{TCommand,TResult}"/>
///         instances from DI (zero or more, in registration order).</item>
///   <item>Exactly one <see cref="ICommandRequestHandler{TCommand,TResult}"/>
///         (throws if missing).</item>
/// </list>
/// </para>
/// <para>
/// <b>Chain construction:</b> The handler is wrapped as the innermost
/// <see cref="CommandHandlerDelegate{TResult}"/>. Each behavior is
/// folded around it in <b>reverse</b> registration order so that the
/// <i>first</i> registered behavior is the outermost wrapper — matching
/// the natural reading order of DI registrations.
/// </para>
/// </summary>
public sealed class CommandExecutionPipeline(
    IServiceProvider serviceProvider,
    ILogger<CommandExecutionPipeline> logger) : ICommandExecutionPipeline
{
    public async Task<CommandResult<TResult>> ExecuteAsync<TCommand, TResult>(
        TCommand command, CancellationToken ct = default)
        where TCommand : ICommandRequest<TResult>
    {
        var commandName = typeof(TCommand).Name;

        // 1. Resolve the inner handler (required)
        var handler = serviceProvider
            .GetService<ICommandRequestHandler<TCommand, TResult>>()
            ?? throw new InvalidOperationException(
                $"No handler registered for command '{commandName}'.");

        // 2. Resolve pipeline behaviors (optional, zero or more)
        var behaviors = serviceProvider
            .GetServices<ICommandPipelineBehavior<TCommand, TResult>>()
            .ToList();

        logger.LogDebug(
            "Executing {Command} through pipeline ({BehaviorCount} behavior(s))",
            commandName, behaviors.Count);

        // 3. Build the chain: handler is the innermost delegate
        CommandHandlerDelegate<TResult> pipeline =
            () => handler.HandleAsync(command, ct);

        // 4. Wrap behaviors in reverse so first-registered = outermost
        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = pipeline; // capture for closure
            pipeline = () => behavior.HandleAsync(command, next, ct);
        }

        // 5. Execute the chain
        var result = await pipeline().ConfigureAwait(false);

        if (!result.Succeeded)
        {
            logger.LogWarning(
                "Command {Command} failed: {Error}",
                commandName, result.ErrorMessage);
        }

        return result;
    }
}
