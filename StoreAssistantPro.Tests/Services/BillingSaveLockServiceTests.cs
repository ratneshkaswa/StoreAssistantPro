using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StoreAssistantPro.Modules.Billing.Services;

namespace StoreAssistantPro.Tests.Services;

public class BillingSaveLockServiceTests
{
    private readonly ILogger<BillingSaveLockService> _logger =
        NullLogger<BillingSaveLockService>.Instance;

    private BillingSaveLockService CreateSut() => new(_logger);

    // ── Initial state ──────────────────────────────────────────────

    [Fact]
    public void InitialState_IsNotLocked()
    {
        var sut = CreateSut();

        Assert.False(sut.IsLocked);
    }

    // ── Single acquire / release ───────────────────────────────────

    [Fact]
    public async Task Acquire_SetsIsLockedTrue()
    {
        var sut = CreateSut();

        await using var guard = await sut.AcquireAsync();

        Assert.True(sut.IsLocked);
    }

    [Fact]
    public async Task Dispose_ReleasesLock()
    {
        var sut = CreateSut();

        var guard = await sut.AcquireAsync();
        await guard.DisposeAsync();

        Assert.False(sut.IsLocked);
    }

    [Fact]
    public async Task AwaitUsing_ReleasesLockAutomatically()
    {
        var sut = CreateSut();

        await using (var guard = await sut.AcquireAsync())
        {
            Assert.True(sut.IsLocked);
        }

        Assert.False(sut.IsLocked);
    }

    // ── Double dispose safety ──────────────────────────────────────

    [Fact]
    public async Task DoubleDispose_DoesNotThrow()
    {
        var sut = CreateSut();

        var guard = await sut.AcquireAsync();
        await guard.DisposeAsync();
        await guard.DisposeAsync(); // second dispose — should be no-op

        Assert.False(sut.IsLocked);
    }

    // ── Serialisation ──────────────────────────────────────────────

    [Fact]
    public async Task SecondAcquire_BlocksUntilFirstReleased()
    {
        var sut = CreateSut();
        var secondAcquired = false;
        var firstReleased = false;

        var guard1 = await sut.AcquireAsync();

        var secondTask = Task.Run(async () =>
        {
            await using var guard2 = await sut.AcquireAsync();
            secondAcquired = true;
            // Verify the first lock was released before we got here
            Assert.True(firstReleased);
        });

        // Give the second task a moment to block
        await Task.Delay(50);
        Assert.False(secondAcquired);

        // Release the first lock
        firstReleased = true;
        await guard1.DisposeAsync();

        // Second task should now complete
        await secondTask;
        Assert.True(secondAcquired);
    }

    [Fact]
    public async Task OnlyOneSaveRunsAtATime()
    {
        var sut = CreateSut();
        var concurrentCount = 0;
        var maxConcurrent = 0;
        var lockObj = new object();

        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            await using var guard = await sut.AcquireAsync();

            lock (lockObj)
            {
                concurrentCount++;
                if (concurrentCount > maxConcurrent)
                    maxConcurrent = concurrentCount;
            }

            // Simulate work
            await Task.Delay(10);

            lock (lockObj)
            {
                concurrentCount--;
            }
        });

        await Task.WhenAll(tasks);

        Assert.Equal(1, maxConcurrent);
    }

    // ── Cancellation ───────────────────────────────────────────────

    [Fact]
    public async Task Acquire_WithCancelledToken_ThrowsOperationCancelled()
    {
        var sut = CreateSut();

        // Hold the lock so the second acquire has to wait
        await using var guard = await sut.AcquireAsync();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.AcquireAsync(cts.Token));
    }

    [Fact]
    public async Task Acquire_CancelledWhileWaiting_ThrowsAndReleasesNothing()
    {
        var sut = CreateSut();

        await using var guard = await sut.AcquireAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.AcquireAsync(cts.Token));

        // Original lock should still be held
        Assert.True(sut.IsLocked);
    }

    // ── Exception safety ───────────────────────────────────────────

    [Fact]
    public async Task Lock_ReleasedEvenIfOperationThrows()
    {
        var sut = CreateSut();

        try
        {
            await using var guard = await sut.AcquireAsync();
            throw new InvalidOperationException("simulated failure");
        }
        catch (InvalidOperationException)
        {
            // expected
        }

        Assert.False(sut.IsLocked);
    }

    [Fact]
    public async Task SequentialAcquireRelease_WorksRepeatedlyAfterFailure()
    {
        var sut = CreateSut();

        // First: acquire, simulate failure, release via dispose
        try
        {
            await using var g1 = await sut.AcquireAsync();
            throw new Exception("fail");
        }
        catch { /* expected */ }

        Assert.False(sut.IsLocked);

        // Second: acquire should succeed immediately
        await using var g2 = await sut.AcquireAsync();
        Assert.True(sut.IsLocked);
    }

    // ── Re-entrancy (same thread re-acquire blocks) ────────────────

    [Fact]
    public async Task ReAcquireOnSameThread_BlocksUntilReleased()
    {
        var sut = CreateSut();
        var guard = await sut.AcquireAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Attempting to re-acquire on a different async context should time out
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Task.Run(() => sut.AcquireAsync(cts.Token)));

        await guard.DisposeAsync();
    }
}
