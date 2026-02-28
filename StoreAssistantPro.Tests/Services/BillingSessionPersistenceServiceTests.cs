using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Services;

namespace StoreAssistantPro.Tests.Services;

public class BillingSessionPersistenceServiceTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly ISessionService _session = Substitute.For<ISessionService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);
    private readonly ILogger<BillingSessionPersistenceService> _logger =
        NullLogger<BillingSessionPersistenceService>.Instance;

    private readonly DateTime _now = new(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
    private int _seedUserId;

    public BillingSessionPersistenceServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _session.CurrentUserType.Returns(UserType.Admin);
        _regional.Now.Returns(_now);

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        using var db = new AppDbContext(_dbOptions);
        var user = new UserCredential
        {
            UserType = UserType.Admin,
            PinHash = "test-hash"
        };
        db.UserCredentials.Add(user);
        db.SaveChanges();
        _seedUserId = user.Id;
    }

    private IDbContextFactory<AppDbContext> CreateFactory()
    {
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new AppDbContext(_dbOptions)));
        return factory;
    }

    private BillingSessionPersistenceService CreateSut() =>
        new(CreateFactory(), _session, _regional, _perf, _logger);

    // ── CreateAsync ────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_InsertsActiveSession()
    {
        var sut = CreateSut();
        var sessionId = Guid.NewGuid();

        var id = await sut.CreateAsync(sessionId, """{"items":[]}""");

        Assert.True(id > 0);

        using var db = new AppDbContext(_dbOptions);
        var row = await db.BillingSessions.FirstOrDefaultAsync(b => b.SessionId == sessionId);
        Assert.NotNull(row);
        Assert.True(row.IsActive);
        Assert.Equal(_seedUserId, row.UserId);
        Assert.Equal("""{"items":[]}""", row.SerializedBillData);
        Assert.Equal(_now, row.CreatedTime);
        Assert.Equal(_now, row.LastUpdated);
    }

    [Fact]
    public async Task CreateAsync_NoUserCredential_Throws()
    {
        _session.CurrentUserType.Returns(UserType.Manager);
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateAsync(Guid.NewGuid(), "{}"));
    }

    // ── UpdateCartAsync ────────────────────────────────────────────

    [Fact]
    public async Task UpdateCartAsync_OverwritesDataAndTimestamp()
    {
        var sut = CreateSut();
        var sessionId = Guid.NewGuid();
        await sut.CreateAsync(sessionId, """{"items":[]}""");

        var later = _now.AddMinutes(5);
        _regional.Now.Returns(later);

        await sut.UpdateCartAsync(sessionId, """{"items":[{"id":1}]}""");

        using var db = new AppDbContext(_dbOptions);
        var row = await db.BillingSessions.FirstAsync(b => b.SessionId == sessionId);
        Assert.Equal("""{"items":[{"id":1}]}""", row.SerializedBillData);
        Assert.Equal(later, row.LastUpdated);
    }

    [Fact]
    public async Task UpdateCartAsync_NoActiveSession_DoesNotThrow()
    {
        var sut = CreateSut();

        var ex = await Record.ExceptionAsync(
            () => sut.UpdateCartAsync(Guid.NewGuid(), "{}"));

        Assert.Null(ex);
    }

    // ── MarkCompletedAsync ─────────────────────────────────────────

    [Fact]
    public async Task MarkCompletedAsync_DeactivatesSession()
    {
        var sut = CreateSut();
        var sessionId = Guid.NewGuid();
        await sut.CreateAsync(sessionId, "{}");

        await sut.MarkCompletedAsync(sessionId);

        using var db = new AppDbContext(_dbOptions);
        var row = await db.BillingSessions.FirstAsync(b => b.SessionId == sessionId);
        Assert.False(row.IsActive);
    }

    // ── MarkCancelledAsync ─────────────────────────────────────────

    [Fact]
    public async Task MarkCancelledAsync_DeactivatesSession()
    {
        var sut = CreateSut();
        var sessionId = Guid.NewGuid();
        await sut.CreateAsync(sessionId, "{}");

        await sut.MarkCancelledAsync(sessionId);

        using var db = new AppDbContext(_dbOptions);
        var row = await db.BillingSessions.FirstAsync(b => b.SessionId == sessionId);
        Assert.False(row.IsActive);
    }

    [Fact]
    public async Task MarkCompleted_UpdatesLastUpdated()
    {
        var sut = CreateSut();
        var sessionId = Guid.NewGuid();
        await sut.CreateAsync(sessionId, "{}");

        var later = _now.AddMinutes(10);
        _regional.Now.Returns(later);

        await sut.MarkCompletedAsync(sessionId);

        using var db = new AppDbContext(_dbOptions);
        var row = await db.BillingSessions.FirstAsync(b => b.SessionId == sessionId);
        Assert.Equal(later, row.LastUpdated);
    }

    // ── GetActiveSessionAsync ──────────────────────────────────────

    [Fact]
    public async Task GetActiveSessionAsync_ReturnsActiveSession()
    {
        var sut = CreateSut();
        var sessionId = Guid.NewGuid();
        await sut.CreateAsync(sessionId, """{"cart":"data"}""");

        var result = await sut.GetActiveSessionAsync(_seedUserId);

        Assert.NotNull(result);
        Assert.Equal(sessionId, result.SessionId);
        Assert.Equal("""{"cart":"data"}""", result.SerializedBillData);
    }

    [Fact]
    public async Task GetActiveSessionAsync_NoActiveSession_ReturnsNull()
    {
        var sut = CreateSut();

        var result = await sut.GetActiveSessionAsync(_seedUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveSessionAsync_CompletedSession_ReturnsNull()
    {
        var sut = CreateSut();
        var sessionId = Guid.NewGuid();
        await sut.CreateAsync(sessionId, "{}");
        await sut.MarkCompletedAsync(sessionId);

        var result = await sut.GetActiveSessionAsync(_seedUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveSessionAsync_ReturnsNewest()
    {
        var sut = CreateSut();

        var older = Guid.NewGuid();
        await sut.CreateAsync(older, """{"v":1}""");

        _regional.Now.Returns(_now.AddMinutes(1));
        var newer = Guid.NewGuid();
        await sut.CreateAsync(newer, """{"v":2}""");

        var result = await sut.GetActiveSessionAsync(_seedUserId);

        Assert.NotNull(result);
        Assert.Equal(newer, result.SessionId);
    }

    // ── PurgeStaleSessionsAsync ────────────────────────────────────

    [Fact]
    public async Task PurgeStaleSessionsAsync_DeletesOldInactiveSessions()
    {
        var sut = CreateSut();

        var sessionId = Guid.NewGuid();
        await sut.CreateAsync(sessionId, "{}");
        await sut.MarkCompletedAsync(sessionId);

        // Move clock forward past the threshold
        _regional.Now.Returns(_now.AddDays(8));

        var deleted = await sut.PurgeStaleSessionsAsync(TimeSpan.FromDays(7));

        Assert.Equal(1, deleted);

        using var db = new AppDbContext(_dbOptions);
        Assert.False(await db.BillingSessions.AnyAsync(b => b.SessionId == sessionId));
    }

    [Fact]
    public async Task PurgeStaleSessionsAsync_KeepsActiveSessions()
    {
        var sut = CreateSut();

        var sessionId = Guid.NewGuid();
        await sut.CreateAsync(sessionId, "{}");

        _regional.Now.Returns(_now.AddDays(8));

        var deleted = await sut.PurgeStaleSessionsAsync(TimeSpan.FromDays(7));

        Assert.Equal(0, deleted);
    }

    [Fact]
    public async Task PurgeStaleSessionsAsync_KeepsRecentInactiveSessions()
    {
        var sut = CreateSut();

        var sessionId = Guid.NewGuid();
        await sut.CreateAsync(sessionId, "{}");
        await sut.MarkCompletedAsync(sessionId);

        // Only 1 day old — inside 7-day threshold
        _regional.Now.Returns(_now.AddDays(1));

        var deleted = await sut.PurgeStaleSessionsAsync(TimeSpan.FromDays(7));

        Assert.Equal(0, deleted);
    }

    // ── ArchiveStaleActiveSessionsAsync ────────────────────────────

    [Fact]
    public async Task ArchiveStaleActive_DeactivatesOldActiveSessions()
    {
        var sut = CreateSut();
        var sessionId = Guid.NewGuid();
        await sut.CreateAsync(sessionId, "{}");

        // Move clock 25 hours ahead (past 24-hour threshold)
        _regional.Now.Returns(_now.AddHours(25));

        var archived = await sut.ArchiveStaleActiveSessionsAsync(TimeSpan.FromHours(24));

        Assert.Equal(1, archived);

        using var db = new AppDbContext(_dbOptions);
        var row = await db.BillingSessions.FirstAsync(b => b.SessionId == sessionId);
        Assert.False(row.IsActive);
    }

    [Fact]
    public async Task ArchiveStaleActive_KeepsRecentActiveSessions()
    {
        var sut = CreateSut();
        var sessionId = Guid.NewGuid();
        await sut.CreateAsync(sessionId, "{}");

        // Only 1 hour old — inside 24-hour threshold
        _regional.Now.Returns(_now.AddHours(1));

        var archived = await sut.ArchiveStaleActiveSessionsAsync(TimeSpan.FromHours(24));

        Assert.Equal(0, archived);

        using var db = new AppDbContext(_dbOptions);
        var row = await db.BillingSessions.FirstAsync(b => b.SessionId == sessionId);
        Assert.True(row.IsActive);
    }

    [Fact]
    public async Task ArchiveStaleActive_IgnoresInactiveSessions()
    {
        var sut = CreateSut();
        var sessionId = Guid.NewGuid();
        await sut.CreateAsync(sessionId, "{}");
        await sut.MarkCompletedAsync(sessionId);

        _regional.Now.Returns(_now.AddHours(25));

        var archived = await sut.ArchiveStaleActiveSessionsAsync(TimeSpan.FromHours(24));

        Assert.Equal(0, archived);
    }

    [Fact]
    public async Task ArchiveStaleActive_UpdatesLastUpdatedTimestamp()
    {
        var sut = CreateSut();
        var sessionId = Guid.NewGuid();
        await sut.CreateAsync(sessionId, "{}");

        var archiveTime = _now.AddHours(25);
        _regional.Now.Returns(archiveTime);

        await sut.ArchiveStaleActiveSessionsAsync(TimeSpan.FromHours(24));

        using var db = new AppDbContext(_dbOptions);
        var row = await db.BillingSessions.FirstAsync(b => b.SessionId == sessionId);
        Assert.Equal(archiveTime, row.LastUpdated);
    }

    [Fact]
    public async Task ArchiveStaleActive_ArchivesMultipleSessions()
    {
        var sut = CreateSut();
        await sut.CreateAsync(Guid.NewGuid(), "{}");

        _regional.Now.Returns(_now.AddMinutes(1));
        await sut.CreateAsync(Guid.NewGuid(), "{}");

        _regional.Now.Returns(_now.AddHours(25));

        var archived = await sut.ArchiveStaleActiveSessionsAsync(TimeSpan.FromHours(24));

        Assert.Equal(2, archived);
    }

    // ── Cleanup ────────────────────────────────────────────────────

    public void Dispose()
    {
        using var db = new AppDbContext(_dbOptions);
        db.Database.EnsureDeleted();
    }
}
