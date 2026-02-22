using StoreAssistantPro.Data;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Executes database operations inside a safe transaction boundary
/// with structured result reporting.
/// <para>
/// <b>Compared to <see cref="ITransactionHelper"/>:</b>
/// <list type="bullet">
///   <item><see cref="ITransactionHelper"/> throws on failure — callers
///         must catch exceptions to handle errors.</item>
///   <item><see cref="ITransactionSafetyService"/> returns a
///         <see cref="TransactionResult"/> — callers inspect
///         <c>Succeeded</c> without exception handling.</item>
/// </list>
/// Both use the same underlying EF Core execution strategy and
/// transaction lifecycle (begin → commit / rollback).
/// </para>
/// <para>
/// <b>Architecture rules:</b>
/// <list type="bullet">
///   <item>All financial writes (sales, billing, refunds, stock
///         adjustments) should go through this service or
///         <see cref="ITransactionHelper"/>.</item>
///   <item>Read-only queries do not need transactions.</item>
///   <item>Prefer the <c>Func&lt;AppDbContext, Task&gt;</c> overloads
///         when the operation writes to the database — this ensures a
///         single context participates in the transaction.</item>
///   <item>Use the <c>Func&lt;Task&gt;</c> overloads for service-layer
///         orchestrations that coordinate multiple services, each
///         managing their own context.</item>
/// </list>
/// </para>
/// <para>
/// Registered as <b>transient</b>. Safe for concurrent calls.
/// </para>
/// </summary>
public interface ITransactionSafetyService
{
    /// <summary>
    /// Runs <paramref name="operation"/> inside a safe boundary.
    /// Catches all exceptions and returns a structured result.
    /// <para>
    /// Use this overload for service-layer orchestrations where
    /// each service resolves its own <see cref="AppDbContext"/>.
    /// </para>
    /// </summary>
    Task<TransactionResult> ExecuteSafeAsync(Func<Task> operation);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a safe boundary and
    /// returns a typed result on success.
    /// </summary>
    Task<TransactionResult<TResult>> ExecuteSafeAsync<TResult>(
        Func<Task<TResult>> operation);

    /// <summary>
    /// Runs <paramref name="operation"/> inside an explicit EF Core
    /// transaction. Commits on success, rolls back on failure.
    /// <para>
    /// Use this overload for writes that must be atomic — the
    /// <see cref="AppDbContext"/> passed to the delegate participates
    /// in a single transaction.
    /// </para>
    /// </summary>
    Task<TransactionResult> ExecuteSafeAsync(
        Func<AppDbContext, Task> operation);

    /// <summary>
    /// Runs <paramref name="operation"/> inside an explicit EF Core
    /// transaction and returns a typed result on success.
    /// </summary>
    Task<TransactionResult<TResult>> ExecuteSafeAsync<TResult>(
        Func<AppDbContext, Task<TResult>> operation);
}
