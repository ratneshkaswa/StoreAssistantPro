namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Thin dispatcher that routes every command through the
/// <see cref="ICommandExecutionPipeline"/>. All registered
/// <see cref="ICommandPipelineBehavior{TCommand,TResult}"/> behaviors
/// execute before the inner handler is invoked.
/// </summary>
public class CommandBus(ICommandExecutionPipeline pipeline) : ICommandBus
{
    /// <inheritdoc/>
    public async Task<CommandResult> SendAsync<TCommand>(TCommand command)
        where TCommand : ICommandRequest<Unit>
    {
        var result = await pipeline
            .ExecuteAsync<TCommand, Unit>(command)
            .ConfigureAwait(false);

        return result.ToBase();
    }

    /// <inheritdoc/>
    public Task<CommandResult<TResult>> SendAsync<TCommand, TResult>(
        TCommand command, CancellationToken ct = default)
        where TCommand : ICommandRequest<TResult>
    {
        return pipeline.ExecuteAsync<TCommand, TResult>(command, ct);
    }
}
