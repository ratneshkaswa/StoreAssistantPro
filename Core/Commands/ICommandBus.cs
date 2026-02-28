namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Dispatches commands to their registered handlers via DI.
/// ViewModels call <see cref="SendAsync{TCommand}"/> instead of
/// calling services directly.
/// <para>
/// <b>All commands execute through the
/// <see cref="ICommandExecutionPipeline"/>:</b> validation, logging,
/// offline, transaction, and performance behaviors wrap every handler
/// invocation regardless of which overload is used.
/// </para>
/// <para>
/// <b>Two overloads:</b>
/// <list type="bullet">
///   <item><see cref="SendAsync{TCommand}(TCommand)"/> — for
///         <see cref="ICommandRequest{TResult}"/> with <see cref="Unit"/>
///         (void) result. Returns untyped <see cref="CommandResult"/>.
///         Used by most existing commands.</item>
///   <item><see cref="SendAsync{TCommand,TResult}(TCommand,CancellationToken)"/>
///         — for <see cref="ICommandRequest{TResult}"/> with a typed
///         result. Returns <see cref="CommandResult{TResult}"/>.</item>
/// </list>
/// </para>
/// </summary>
public interface ICommandBus
{
    /// <summary>
    /// Dispatches a <see cref="ICommandRequest{Unit}"/> through the
    /// full pipeline and returns an untyped <see cref="CommandResult"/>.
    /// </summary>
    Task<CommandResult> SendAsync<TCommand>(TCommand command)
        where TCommand : ICommandRequest<Unit>;

    /// <summary>
    /// Dispatches an <see cref="ICommandRequest{TResult}"/> through
    /// the full pipeline (behaviors → handler) and returns a typed result.
    /// </summary>
    Task<CommandResult<TResult>> SendAsync<TCommand, TResult>(
        TCommand command, CancellationToken ct = default)
        where TCommand : ICommandRequest<TResult>;
}
