using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Commands.Transaction;

/// <summary>
/// Pipeline behavior that wraps command execution in a
/// <see cref="ITransactionSafetyService"/> boundary. Commits on
/// success, rolls back on failure.
/// <para>
/// <b>Opt-in via marker:</b> Only commands that implement
/// <see cref="ITransactionalCommand"/> are wrapped. All other
/// commands pass through to <c>next()</c> with zero overhead.
/// </para>
/// <para>
/// <b>Transaction mapping:</b>
/// <list type="bullet">
///   <item><see cref="TransactionResult.Succeeded"/> →
///         <see cref="CommandResult{TResult}"/> from inner pipeline
///         (passed through unchanged).</item>
///   <item><see cref="TransactionResult"/> failure →
///         <see cref="CommandResult{TResult}.Failure"/> with the
///         transaction error message.</item>
/// </list>
/// </para>
/// <para>
/// <b>Pipeline position:</b> Register <b>after</b> validation and
/// logging so that:
/// <list type="number">
///   <item>Invalid commands are rejected before a transaction opens.</item>
///   <item>Logging captures the full duration including transaction
///         overhead.</item>
///   <item>The transaction wraps only the inner handler (and any
///         remaining behaviors).</item>
/// </list>
/// <code>
/// services.AddTransient(typeof(ICommandPipelineBehavior&lt;,&gt;),
///     typeof(ValidationPipelineBehavior&lt;,&gt;));
/// services.AddTransient(typeof(ICommandPipelineBehavior&lt;,&gt;),
///     typeof(LoggingPipelineBehavior&lt;,&gt;));
/// services.AddTransient(typeof(ICommandPipelineBehavior&lt;,&gt;),
///     typeof(TransactionPipelineBehavior&lt;,&gt;));
/// </code>
/// </para>
/// </summary>
public sealed class TransactionPipelineBehavior<TCommand, TResult>(
    ITransactionSafetyService transactionSafety,
    ILogger<TransactionPipelineBehavior<TCommand, TResult>> logger)
    : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommandRequest<TResult>
{
    /// <summary>
    /// Cached check — <c>true</c> when <typeparamref name="TCommand"/>
    /// implements <see cref="ITransactionalCommand"/>. Evaluated once
    /// per closed generic type.
    /// </summary>
    private static readonly bool IsTransactional =
        typeof(ITransactionalCommand).IsAssignableFrom(typeof(TCommand));

    private static readonly string CommandName = typeof(TCommand).Name;

    public async Task<CommandResult<TResult>> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResult> next,
        CancellationToken ct = default)
    {
        if (!IsTransactional)
            return await next().ConfigureAwait(false);

        logger.LogDebug("Wrapping {Command} in transaction", CommandName);

        var txResult = await transactionSafety.ExecuteSafeAsync(async () =>
        {
            var innerResult = await next().ConfigureAwait(false);

            if (!innerResult.Succeeded)
            {
                // Throw so TransactionSafetyService rolls back and
                // captures the error in TransactionResult.
                throw new CommandExecutionException(
                    innerResult.ErrorMessage ?? "Command failed");
            }

            return innerResult;
        }).ConfigureAwait(false);

        if (txResult.Succeeded)
        {
            logger.LogDebug("Transaction committed for {Command}", CommandName);
            return txResult.Value!;
        }

        // Map transaction failure → command failure
        var error = txResult.ErrorMessage ?? "Transaction failed";

        if (txResult.IsConcurrencyConflict)
        {
            logger.LogWarning(
                "Concurrency conflict in {Command}: {Error}",
                CommandName, error);
        }
        else if (txResult.IsConstraintViolation)
        {
            logger.LogWarning(
                "Constraint violation in {Command}: {Error}",
                CommandName, error);
        }
        else if (txResult.IsCancelled)
        {
            logger.LogInformation(
                "Transaction cancelled for {Command}", CommandName);
        }
        else
        {
            logger.LogWarning(
                "Transaction failed for {Command}: {Error}",
                CommandName, error);
        }

        return CommandResult<TResult>.Failure(error);
    }

    /// <summary>
    /// Internal exception used to signal command failure to
    /// <see cref="ITransactionSafetyService"/> so it rolls back
    /// the transaction. Never escapes the behavior.
    /// </summary>
    private sealed class CommandExecutionException(string message)
        : Exception(message);
}
