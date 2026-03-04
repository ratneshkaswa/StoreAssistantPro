namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Thin dispatcher that routes every command through the
/// <see cref="ICommandExecutionPipeline"/>. All registered
/// <see cref="ICommandPipelineBehavior{TCommand,TResult}"/> behaviors
/// execute before the inner handler is invoked.
/// <para>
/// <b>Threading:</b> Public methods do NOT use ConfigureAwait(false)
/// so that ViewModel callers resume on the UI thread and can safely
/// update bindings / close windows after awaiting.
/// </para>
/// </summary>
public class CommandBus(ICommandExecutionPipeline pipeline) : ICommandBus
{
    /// <inheritdoc/>
    public async Task<CommandResult> SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommandRequest<Unit>
    {
        var result = await pipeline
            .ExecuteAsync<TCommand, Unit>(command, ct);

        return result.ToBase();
    }

    /// <inheritdoc/>
    public async Task<CommandResult<TResult>> SendAsync<TCommand, TResult>(
        TCommand command, CancellationToken ct = default)
        where TCommand : ICommandRequest<TResult>
    {
        return await pipeline.ExecuteAsync<TCommand, TResult>(command, ct);
    }
}
