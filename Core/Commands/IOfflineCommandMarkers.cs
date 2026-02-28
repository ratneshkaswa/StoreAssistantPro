namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Marker interface for commands that <b>require</b> database
/// connectivity and must be blocked while the application is offline.
/// <para>
/// <b>When to use:</b> Apply to any
/// <see cref="ICommandRequest{TResult}"/> that performs direct
/// database reads or writes and has no offline fallback — e.g.
/// product management, user administration, tax configuration.
/// </para>
/// <para>
/// <b>Behavior:</b> When the application is offline and the command
/// implements this interface, the
/// <see cref="Offline.OfflinePipelineBehavior{TCommand,TResult}"/>
/// short-circuits the pipeline and returns
/// <see cref="CommandResult{TResult}.Failure"/> with a user-facing
/// message. The inner handler is never called.
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// public sealed record UpdateProductCommand(int Id, string Name, decimal Price)
///     : ICommandRequest&lt;bool&gt;, IOnlineOnlyCommand;
/// </code>
/// </para>
/// </summary>
public interface IOnlineOnlyCommand;

/// <summary>
/// Marker interface for commands that support <b>offline execution</b>.
/// The handler is responsible for its own offline strategy (e.g.
/// enqueuing to <see cref="Sales.Services.IOfflineBillingQueue"/>).
/// <para>
/// <b>When to use:</b> Apply to any
/// <see cref="ICommandRequest{TResult}"/> that can operate without
/// database connectivity — e.g. completing a sale (queued offline).
/// </para>
/// <para>
/// <b>Behavior:</b> When the application is offline and the command
/// implements this interface, the
/// <see cref="Offline.OfflinePipelineBehavior{TCommand,TResult}"/>
/// lets the command pass through to the handler. The handler itself
/// checks <see cref="Services.IOfflineModeService.IsOffline"/> to
/// decide whether to use the online or offline path.
/// </para>
/// <para>
/// <b>Commands with neither marker:</b> pass through the behavior
/// unchanged regardless of connectivity — they are unaffected by
/// offline mode.
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// public sealed record CompleteSaleCommand(…)
///     : ICommandRequest&lt;int&gt;, IOfflineCapableCommand;
/// </code>
/// </para>
/// </summary>
public interface IOfflineCapableCommand;
