using Microsoft.Extensions.Logging;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Process-level async mutex that serialises billing save operations.
/// Uses <see cref="SemaphoreSlim"/>(1,1) so only one save can run
/// at a time. The lock is always released via the disposable guard,
/// even if the save throws.
/// </summary>
public class BillingSaveLockService(
    ILogger<BillingSaveLockService> logger) : IBillingSaveLockService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public bool IsLocked => _semaphore.CurrentCount == 0;

    public async Task<IAsyncDisposable> AcquireAsync(CancellationToken ct = default)
    {
        logger.LogDebug("Waiting to acquire billing save lock");

        await _semaphore.WaitAsync(ct).ConfigureAwait(false);

        logger.LogDebug("Billing save lock acquired");
        return new LockGuard(_semaphore, logger);
    }

    /// <summary>
    /// Disposable guard that releases the semaphore exactly once
    /// when <see cref="DisposeAsync"/> is called.
    /// </summary>
    private sealed class LockGuard(
        SemaphoreSlim semaphore,
        ILogger logger) : IAsyncDisposable
    {
        private int _released;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                semaphore.Release();
                logger.LogDebug("Billing save lock released");
            }

            return ValueTask.CompletedTask;
        }
    }
}
