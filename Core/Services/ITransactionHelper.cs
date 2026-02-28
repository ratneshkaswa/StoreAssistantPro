using StoreAssistantPro.Data;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Executes a unit of work inside an explicit database transaction
/// with SQL Server execution strategy retry support.
/// <para>
/// <b>Architecture rule:</b> All financial writes (sales, refunds,
/// billing, stock adjustments) must go through this helper.
/// Read-only queries do not need transactions.
/// </para>
/// </summary>
public interface ITransactionHelper
{
    /// <summary>
    /// Runs <paramref name="operation"/> inside a transaction.
    /// Commits on success, rolls back on exception, retries on
    /// transient SQL errors.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<AppDbContext, Task> operation);

    /// <summary>
    /// Same as <see cref="ExecuteInTransactionAsync(Func{AppDbContext, Task})"/>
    /// but returns a result.
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<AppDbContext, Task<TResult>> operation);
}
