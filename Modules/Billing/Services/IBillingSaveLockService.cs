namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Serialises billing save operations so that at most one
/// <c>CreateSaleAsync</c> call executes at a time.
/// <para>
/// <b>Why a dedicated service?</b><br/>
/// The ViewModel already has an <c>IsSaving</c> UI guard that disables
/// the button, and the database has an idempotency-key unique index.
/// This service adds a <b>process-level</b> guarantee: even if two
/// code-paths (e.g. keyboard shortcut + button click, or two
/// ViewModels) both try to save concurrently, only one proceeds
/// while the other waits.
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// await using var guard = await saveLock.AcquireAsync(ct);
/// // … perform the save …
/// // guard.Dispose releases the lock automatically
/// </code>
/// </para>
/// <para>
/// Registered as a <b>singleton</b> — one lock for the whole process.
/// </para>
/// </summary>
public interface IBillingSaveLockService
{
    /// <summary>
    /// Acquires the save lock asynchronously. Returns an
    /// <see cref="IAsyncDisposable"/> guard that releases the lock
    /// when disposed. The lock is always released, even on exception.
    /// </summary>
    /// <param name="ct">
    /// Cancellation token. If cancelled while waiting, throws
    /// <see cref="OperationCanceledException"/>.
    /// </param>
    Task<IAsyncDisposable> AcquireAsync(CancellationToken ct = default);

    /// <summary>
    /// <c>true</c> when a save operation currently holds the lock.
    /// Observable for diagnostic / status-bar purposes.
    /// </summary>
    bool IsLocked { get; }
}
