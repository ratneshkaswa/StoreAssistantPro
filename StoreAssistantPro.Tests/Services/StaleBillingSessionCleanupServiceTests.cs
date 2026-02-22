using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Billing.Services;

namespace StoreAssistantPro.Tests.Services;

public class StaleBillingSessionCleanupServiceTests : IDisposable
{
    private readonly IBillingSessionPersistenceService _persistence =
        Substitute.For<IBillingSessionPersistenceService>();
    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

    private static readonly TimeSpan StaleThreshold = TimeSpan.FromHours(24);
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(7);

    private StaleBillingSessionCleanupService CreateSut(
        TimeSpan? staleThreshold = null,
        TimeSpan? retention = null) =>
        new(_persistence, _perf,
            NullLogger<StaleBillingSessionCleanupService>.Instance,
            staleThreshold ?? StaleThreshold,
            retention ?? RetentionPeriod,
            enableTimer: false);

    // ── Thresholds ─────────────────────────────────────────────────

    [Fact]
    public void StaleActiveThreshold_ReturnsConfiguredValue()
    {
        var sut = CreateSut(staleThreshold: TimeSpan.FromHours(12));

        Assert.Equal(TimeSpan.FromHours(12), sut.StaleActiveThreshold);
    }

    [Fact]
    public void InactiveRetentionPeriod_ReturnsConfiguredValue()
    {
        var sut = CreateSut(retention: TimeSpan.FromDays(14));

        Assert.Equal(TimeSpan.FromDays(14), sut.InactiveRetentionPeriod);
    }

    // ── RunCleanupAsync — both phases called ───────────────────────

    [Fact]
    public async Task RunCleanup_CallsArchiveThenPurge()
    {
        _persistence.ArchiveStaleActiveSessionsAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(2);
        _persistence.PurgeStaleSessionsAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(5);

        var sut = CreateSut();

        var (archived, purged) = await sut.RunCleanupAsync();

        Assert.Equal(2, archived);
        Assert.Equal(5, purged);
    }

    [Fact]
    public async Task RunCleanup_PassesCorrectThresholds()
    {
        var customStale = TimeSpan.FromHours(6);
        var customRetention = TimeSpan.FromDays(3);

        _persistence.ArchiveStaleActiveSessionsAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _persistence.PurgeStaleSessionsAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var sut = CreateSut(staleThreshold: customStale, retention: customRetention);
        await sut.RunCleanupAsync();

        await _persistence.Received(1)
            .ArchiveStaleActiveSessionsAsync(customStale, Arg.Any<CancellationToken>());
        await _persistence.Received(1)
            .PurgeStaleSessionsAsync(customRetention, Arg.Any<CancellationToken>());
    }

    // ── Nothing to clean ───────────────────────────────────────────

    [Fact]
    public async Task RunCleanup_NothingStale_ReturnsZeros()
    {
        _persistence.ArchiveStaleActiveSessionsAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _persistence.PurgeStaleSessionsAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var sut = CreateSut();

        var (archived, purged) = await sut.RunCleanupAsync();

        Assert.Equal(0, archived);
        Assert.Equal(0, purged);
    }

    // ── Archive failure does not block purge ────────────────────────

    [Fact]
    public async Task RunCleanup_ArchiveThrows_PurgeStillRuns()
    {
        _persistence.ArchiveStaleActiveSessionsAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB error"));
        _persistence.PurgeStaleSessionsAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(3);

        var sut = CreateSut();

        var (archived, purged) = await sut.RunCleanupAsync();

        Assert.Equal(0, archived);
        Assert.Equal(3, purged);
    }

    // ── Purge failure does not throw ───────────────────────────────

    [Fact]
    public async Task RunCleanup_PurgeThrows_DoesNotBubble()
    {
        _persistence.ArchiveStaleActiveSessionsAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(1);
        _persistence.PurgeStaleSessionsAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var sut = CreateSut();

        var (archived, purged) = await sut.RunCleanupAsync();

        Assert.Equal(1, archived);
        Assert.Equal(0, purged);
    }

    // ── Both fail — still no throw ─────────────────────────────────

    [Fact]
    public async Task RunCleanup_BothFail_DoesNotBubble()
    {
        _persistence.ArchiveStaleActiveSessionsAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB error 1"));
        _persistence.PurgeStaleSessionsAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB error 2"));

        var sut = CreateSut();

        var ex = await Record.ExceptionAsync(() => sut.RunCleanupAsync());

        Assert.Null(ex);
    }

    // ── Idempotency — safe to call multiple times ──────────────────

    [Fact]
    public async Task RunCleanup_CalledTwice_BothSucceed()
    {
        _persistence.ArchiveStaleActiveSessionsAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(1, 0);
        _persistence.PurgeStaleSessionsAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(2, 0);

        var sut = CreateSut();

        var first = await sut.RunCleanupAsync();
        var second = await sut.RunCleanupAsync();

        Assert.Equal((1, 2), first);
        Assert.Equal((0, 0), second);
    }

    // ── Dispose ────────────────────────────────────────────────────

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var sut = CreateSut();

        var ex = Record.Exception(() => sut.Dispose());

        Assert.Null(ex);
    }

    public void Dispose()
    {
        // no-op; SUT is created per test
    }
}
