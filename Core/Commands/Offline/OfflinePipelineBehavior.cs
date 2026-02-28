using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Commands.Offline;

/// <summary>
/// Pipeline behavior that enforces offline-mode rules on commands
/// flowing through the enterprise pipeline.
/// <para>
/// <b>Decision matrix:</b>
/// <code>
///                        ┌──────────────┬──────────────┐
///                        │   Online     │   Offline    │
/// ┌──────────────────────┼──────────────┼──────────────┤
/// │ IOnlineOnlyCommand   │  next()      │  BLOCK ✘     │
/// │ IOfflineCapableCommand│  next()     │  next() ✔    │
/// │ (no marker)          │  next()      │  next()      │
/// └──────────────────────┴──────────────┴──────────────┘
/// </code>
/// </para>
/// <para>
/// <b>Block behavior:</b> Returns
/// <see cref="CommandResult{TResult}.Failure"/> with a user-facing
/// message. The inner handler is never invoked.
/// </para>
/// <para>
/// <b>Pass-through behavior:</b> For
/// <see cref="IOfflineCapableCommand"/> commands, the behavior lets
/// the command through to the handler which implements its own
/// offline strategy (e.g. enqueuing to
/// <see cref="Sales.Services.IOfflineBillingQueue"/>). The behavior
/// logs that offline execution is occurring.
/// </para>
/// <para>
/// <b>Pipeline position:</b> Register <b>after</b> validation and
/// <b>before</b> transaction, so that:
/// <list type="number">
///   <item>Invalid commands are rejected before the offline check.</item>
///   <item>Online-only commands are blocked before a transaction opens.</item>
///   <item>Offline-capable commands skip the transaction behavior
///         (since they won't hit the DB anyway).</item>
/// </list>
/// <code>
/// services.AddTransient(typeof(ICommandPipelineBehavior&lt;,&gt;),
///     typeof(ValidationPipelineBehavior&lt;,&gt;));
/// services.AddTransient(typeof(ICommandPipelineBehavior&lt;,&gt;),
///     typeof(LoggingPipelineBehavior&lt;,&gt;));
/// services.AddTransient(typeof(ICommandPipelineBehavior&lt;,&gt;),
///     typeof(OfflinePipelineBehavior&lt;,&gt;));          // ← HERE
/// services.AddTransient(typeof(ICommandPipelineBehavior&lt;,&gt;),
///     typeof(TransactionPipelineBehavior&lt;,&gt;));
/// </code>
/// </para>
/// </summary>
public sealed class OfflinePipelineBehavior<TCommand, TResult>(
    IOfflineModeService offlineMode,
    ILogger<OfflinePipelineBehavior<TCommand, TResult>> logger)
    : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommandRequest<TResult>
{
    /// <summary>
    /// Cached marker checks — evaluated once per closed generic type.
    /// </summary>
    private static readonly bool IsOnlineOnly =
        typeof(IOnlineOnlyCommand).IsAssignableFrom(typeof(TCommand));

    private static readonly bool IsOfflineCapable =
        typeof(IOfflineCapableCommand).IsAssignableFrom(typeof(TCommand));

    private static readonly string CommandName = typeof(TCommand).Name;

    public async Task<CommandResult<TResult>> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResult> next,
        CancellationToken ct = default)
    {
        // Online — all commands pass through regardless of marker
        if (!offlineMode.IsOffline)
            return await next().ConfigureAwait(false);

        // Offline + IOnlineOnlyCommand → block
        if (IsOnlineOnly)
        {
            logger.LogWarning(
                "Command {Command} blocked: requires database connectivity",
                CommandName);

            return CommandResult<TResult>.Failure(
                "This operation requires an active database connection. " +
                "Please try again when connectivity is restored.");
        }

        // Offline + IOfflineCapableCommand → let handler decide
        if (IsOfflineCapable)
        {
            logger.LogInformation(
                "Executing {Command} in offline mode", CommandName);

            return await next().ConfigureAwait(false);
        }

        // Offline + no marker → pass through (unmarked commands are unaffected)
        return await next().ConfigureAwait(false);
    }
}
