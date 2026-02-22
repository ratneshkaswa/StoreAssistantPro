using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Data;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Executes database operations inside a safe transaction boundary.
/// <para>
/// <b>Two flavours:</b>
/// </para>
/// <list type="number">
///   <item><b>Service-layer (<c>Func&lt;Task&gt;</c>):</b> Wraps the
///         operation in a try/catch and returns a structured
///         <see cref="TransactionResult"/>. No explicit EF transaction
///         is created — each service in the delegate manages its own
///         context. Use for orchestrating multiple service calls.</item>
///   <item><b>Transactional (<c>Func&lt;AppDbContext, Task&gt;</c>):</b>
///         Creates a short-lived <see cref="AppDbContext"/> inside an
///         explicit EF Core transaction with SQL Server execution
///         strategy retry support. The delegate receives this context,
///         and <see cref="DbContext.SaveChangesAsync()"/> + commit are
///         handled automatically. Use for atomic multi-table writes.</item>
/// </list>
/// <para>
/// <b>Lifecycle events:</b> Every operation publishes
/// <see cref="TransactionStartedEvent"/>, then either
/// <see cref="TransactionCommittedEvent"/> or
/// <see cref="TransactionFailedEvent"/> via <see cref="IEventBus"/>.
/// Event publishing failures are swallowed — they never affect
/// the transaction outcome.
/// </para>
/// </summary>
public class TransactionSafetyService(
    IDbContextFactory<AppDbContext> contextFactory,
    IEventBus eventBus,
    IPerformanceMonitor perf,
    ILogger<TransactionSafetyService> logger) : ITransactionSafetyService
{
    // ── Service-layer (no shared context) ──────────────────────────

    public async Task<TransactionResult> ExecuteSafeAsync(Func<Task> operation)
    {
        const string scope = "Service-layer operation";
        using var _ = perf.BeginScope("TransactionSafety.ExecuteSafe");
        var (operationId, sw) = await BeginLifecycleAsync(scope).ConfigureAwait(false);

        try
        {
            await operation().ConfigureAwait(false);

            logger.LogDebug("Service-layer operation completed successfully");
            var result = TransactionResult.Success();
            await PublishCommittedAsync(operationId, scope, sw.Elapsed).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            return await ClassifyAndPublishFailureAsync(ex, scope, operationId, sw.Elapsed)
                .ConfigureAwait(false);
        }
    }

    public async Task<TransactionResult<TResult>> ExecuteSafeAsync<TResult>(
        Func<Task<TResult>> operation)
    {
        const string scope = "Service-layer operation";
        using var _ = perf.BeginScope("TransactionSafety.ExecuteSafe<T>");
        var (operationId, sw) = await BeginLifecycleAsync(scope).ConfigureAwait(false);

        try
        {
            var result = await operation().ConfigureAwait(false);

            logger.LogDebug("Service-layer operation completed successfully");
            var txResult = TransactionResult<TResult>.Success(result);
            await PublishCommittedAsync(operationId, scope, sw.Elapsed).ConfigureAwait(false);
            return txResult;
        }
        catch (Exception ex)
        {
            return await ClassifyAndPublishFailureAsync<TResult>(ex, scope, operationId, sw.Elapsed)
                .ConfigureAwait(false);
        }
    }

    // ── Transactional (shared context with explicit transaction) ───

    public async Task<TransactionResult> ExecuteSafeAsync(
        Func<AppDbContext, Task> operation)
    {
        return await ExecuteTransactionalCoreAsync(operation).ConfigureAwait(false);
    }

    public async Task<TransactionResult<TResult>> ExecuteSafeAsync<TResult>(
        Func<AppDbContext, Task<TResult>> operation)
    {
        return await ExecuteTransactionalCoreAsync(operation).ConfigureAwait(false);
    }

    // ── Core transactional engine ──────────────────────────────────

    private async Task<TransactionResult> ExecuteTransactionalCoreAsync(
        Func<AppDbContext, Task> operation)
    {
        const string scope = "Transaction";
        using var _ = perf.BeginScope("TransactionSafety.Transactional");
        var (operationId, sw) = await BeginLifecycleAsync(scope).ConfigureAwait(false);

        try
        {
            await using var strategySource = await contextFactory
                .CreateDbContextAsync().ConfigureAwait(false);
            var strategy = strategySource.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var context = await contextFactory
                    .CreateDbContextAsync().ConfigureAwait(false);
                await using var transaction = await context.Database
                    .BeginTransactionAsync().ConfigureAwait(false);

                try
                {
                    await operation(context).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    await transaction.CommitAsync().ConfigureAwait(false);
                }
                catch
                {
                    await RollbackSafelyAsync(transaction).ConfigureAwait(false);
                    throw;
                }
            }).ConfigureAwait(false);

            logger.LogInformation("Transaction committed successfully");
            var result = TransactionResult.Success();
            await PublishCommittedAsync(operationId, scope, sw.Elapsed).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            return await ClassifyAndPublishFailureAsync(ex, scope, operationId, sw.Elapsed)
                .ConfigureAwait(false);
        }
    }

    private async Task<TransactionResult<TResult>> ExecuteTransactionalCoreAsync<TResult>(
        Func<AppDbContext, Task<TResult>> operation)
    {
        const string scope = "Transaction";
        using var _ = perf.BeginScope("TransactionSafety.Transactional<T>");
        var (operationId, sw) = await BeginLifecycleAsync(scope).ConfigureAwait(false);

        try
        {
            await using var strategySource = await contextFactory
                .CreateDbContextAsync().ConfigureAwait(false);
            var strategy = strategySource.Database.CreateExecutionStrategy();

            var result = await strategy.ExecuteAsync(async () =>
            {
                await using var context = await contextFactory
                    .CreateDbContextAsync().ConfigureAwait(false);
                await using var transaction = await context.Database
                    .BeginTransactionAsync().ConfigureAwait(false);

                try
                {
                    var value = await operation(context).ConfigureAwait(false);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                    await transaction.CommitAsync().ConfigureAwait(false);
                    return value;
                }
                catch
                {
                    await RollbackSafelyAsync(transaction).ConfigureAwait(false);
                    throw;
                }
            }).ConfigureAwait(false);

            logger.LogInformation("Transaction committed successfully");
            var txResult = TransactionResult<TResult>.Success(result);
            await PublishCommittedAsync(operationId, scope, sw.Elapsed).ConfigureAwait(false);
            return txResult;
        }
        catch (Exception ex)
        {
            return await ClassifyAndPublishFailureAsync<TResult>(ex, scope, operationId, sw.Elapsed)
                .ConfigureAwait(false);
        }
    }

    // ── Error classification ───────────────────────────────────────

    private TransactionResult ClassifyFailure(Exception ex, string scope)
    {
        var c = Classify(ex, scope);
        return TransactionResult.Failure(c.Message, ex,
            isConcurrencyConflict: c.IsConcurrency,
            isConstraintViolation: c.IsConstraint,
            isCancelled: c.IsCancelled);
    }

    private TransactionResult<TResult> ClassifyFailure<TResult>(Exception ex, string scope)
    {
        var c = Classify(ex, scope);
        return TransactionResult<TResult>.Failure(c.Message, ex,
            isConcurrencyConflict: c.IsConcurrency,
            isConstraintViolation: c.IsConstraint,
            isCancelled: c.IsCancelled);
    }

    private async Task<TransactionResult> ClassifyAndPublishFailureAsync(
        Exception ex, string scope, Guid operationId, TimeSpan elapsed)
    {
        var result = ClassifyFailure(ex, scope);
        await PublishFailedAsync(operationId, scope, elapsed, result).ConfigureAwait(false);
        return result;
    }

    private async Task<TransactionResult<TResult>> ClassifyAndPublishFailureAsync<TResult>(
        Exception ex, string scope, Guid operationId, TimeSpan elapsed)
    {
        var result = ClassifyFailure<TResult>(ex, scope);
        await PublishFailedAsync(operationId, scope, elapsed, result).ConfigureAwait(false);
        return result;
    }

    private FailureClassification Classify(Exception ex, string scope)
    {
        return ex switch
        {
            OperationCanceledException =>
                LogAndReturn(LogLevel.Information, ex,
                    "{Scope} was cancelled", scope,
                    "The operation was cancelled.",
                    isCancelled: true),

            DbUpdateConcurrencyException =>
                LogAndReturn(LogLevel.Warning, ex,
                    "Concurrency conflict during {Scope}", scope,
                    "Data was modified by another user. Please try again.",
                    isConcurrency: true),

            DbUpdateException =>
                LogAndReturn(LogLevel.Error, ex,
                    "{Scope} failed — database constraint violation", scope,
                    "The operation could not be completed due to a data conflict. Please try again.",
                    isConstraint: true),

            InvalidOperationException =>
                LogAndReturn(LogLevel.Error, ex,
                    "{Scope} failed — {Message}", scope,
                    ex.Message, exMessageArg: ex.Message),

            _ =>
                LogAndReturn(LogLevel.Error, ex,
                    "{Scope} failed — rolled back", scope,
                    "An unexpected error occurred. Please try again or contact support.")
        };
    }

    private FailureClassification LogAndReturn(
        LogLevel level, Exception ex,
        string logTemplate, string scope, string userMessage,
        bool isConcurrency = false, bool isConstraint = false,
        bool isCancelled = false, string? exMessageArg = null)
    {
        if (exMessageArg is not null)
            logger.Log(level, ex, logTemplate, scope, exMessageArg);
        else
            logger.Log(level, ex, logTemplate, scope);

        return new(userMessage, isConcurrency, isConstraint, isCancelled);
    }

    private readonly record struct FailureClassification(
        string Message, bool IsConcurrency, bool IsConstraint, bool IsCancelled);

    // ── Rollback helper ────────────────────────────────────────────

    private async Task RollbackSafelyAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
            logger.LogDebug("Transaction rolled back explicitly");
        }
        catch (Exception rollbackEx)
        {
            logger.LogWarning(rollbackEx, "Rollback itself failed — transaction will be disposed");
        }
    }

    // ── Lifecycle event helpers ────────────────────────────────────

    private async Task<(Guid OperationId, Stopwatch Stopwatch)> BeginLifecycleAsync(string scope)
    {
        var operationId = Guid.NewGuid();
        var sw = Stopwatch.StartNew();
        await PublishEventSafeAsync(new TransactionStartedEvent(operationId, scope))
            .ConfigureAwait(false);
        return (operationId, sw);
    }

    private Task PublishCommittedAsync(Guid operationId, string scope, TimeSpan elapsed) =>
        PublishEventSafeAsync(new TransactionCommittedEvent(operationId, scope, elapsed));

    private Task PublishFailedAsync(
        Guid operationId, string scope, TimeSpan elapsed, TransactionResult result) =>
        PublishEventSafeAsync(new TransactionFailedEvent(
            operationId, scope, elapsed,
            result.ErrorMessage ?? "Unknown error",
            result.IsConcurrencyConflict,
            result.IsConstraintViolation,
            result.IsCancelled));

    private Task PublishFailedAsync<TResult>(
        Guid operationId, string scope, TimeSpan elapsed, TransactionResult<TResult> result) =>
        PublishEventSafeAsync(new TransactionFailedEvent(
            operationId, scope, elapsed,
            result.ErrorMessage ?? "Unknown error",
            result.IsConcurrencyConflict,
            result.IsConstraintViolation,
            result.IsCancelled));

    private async Task PublishEventSafeAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        try
        {
            await eventBus.PublishAsync(@event).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish {EventType} — event swallowed", typeof(TEvent).Name);
        }
    }
}
