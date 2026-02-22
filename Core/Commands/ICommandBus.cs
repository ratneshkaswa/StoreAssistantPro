namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Dispatches commands to their registered handlers via DI.
/// ViewModels call <see cref="SendAsync{TCommand}"/> instead of
/// calling services directly.
/// <para>
/// <b>Two dispatch paths:</b>
/// <list type="bullet">
///   <item><see cref="SendAsync{TCommand}(TCommand)"/> — legacy path
///         for <see cref="ICommand"/>. Resolves
///         <see cref="ICommandHandler{TCommand}"/> directly.</item>
///   <item><see cref="SendAsync{TCommand,TResult}(TCommand,CancellationToken)"/>
///         — enterprise path for <see cref="ICommandRequest{TResult}"/>.
///         Executes through the <see cref="ICommandExecutionPipeline"/>
///         (behaviors → handler).</item>
/// </list>
/// </para>
/// </summary>
public interface ICommandBus
{
    /// <summary>
    /// Dispatches a simple <see cref="ICommand"/> to its handler.
    /// No pipeline behaviors are applied.
    /// </summary>
    Task<CommandResult> SendAsync<TCommand>(TCommand command)
        where TCommand : ICommand;

    /// <summary>
    /// Dispatches an <see cref="ICommandRequest{TResult}"/> through
    /// the full pipeline (behaviors → handler) and returns a typed result.
    /// </summary>
    Task<CommandResult<TResult>> SendAsync<TCommand, TResult>(
        TCommand command, CancellationToken ct = default)
        where TCommand : ICommandRequest<TResult>;
}
