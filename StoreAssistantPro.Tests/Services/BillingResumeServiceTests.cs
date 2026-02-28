using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Models;
using StoreAssistantPro.Modules.Billing.Services;

namespace StoreAssistantPro.Tests.Services;

public class BillingResumeServiceTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly ISessionService _session = Substitute.For<ISessionService>();
    private readonly IBillingSessionPersistenceService _persistence =
        Substitute.For<IBillingSessionPersistenceService>();
    private readonly IBillingSessionService _billingSession =
        Substitute.For<IBillingSessionService>();
    private readonly IBillingSessionRestoreService _restoreService =
        Substitute.For<IBillingSessionRestoreService>();
    private readonly IBillingModeService _modeService =
        Substitute.For<IBillingModeService>();
    private readonly IFocusLockService _focusLock =
        Substitute.For<IFocusLockService>();
    private readonly IDialogService _dialog = Substitute.For<IDialogService>();
    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

    private int _seedUserId;

    public BillingResumeServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _session.CurrentUserType.Returns(UserType.Admin);

        // Default: integration chain healthy after resume
        _modeService.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(true);

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

    private BillingResumeService CreateSut() =>
        new(CreateFactory(), _session, _persistence, _billingSession,
            _restoreService, _modeService, _focusLock, _dialog, _perf,
            NullLogger<BillingResumeService>.Instance);

    // ── No active session ──────────────────────────────────────────

    [Fact]
    public async Task TryResume_NoActiveSession_ReturnsNoSession()
    {
        _persistence.GetActiveSessionAsync(_seedUserId, Arg.Any<CancellationToken>())
            .Returns((BillingSession?)null);

        var result = await CreateSut().TryResumeAsync();

        Assert.Equal(ResumeOutcome.NoSession, result.Outcome);
        Assert.Null(result.Session);
    }

    [Fact]
    public async Task TryResume_NoActiveSession_DoesNotPrompt()
    {
        _persistence.GetActiveSessionAsync(_seedUserId, Arg.Any<CancellationToken>())
            .Returns((BillingSession?)null);

        await CreateSut().TryResumeAsync();

        _dialog.DidNotReceive().ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>());
    }

    [Fact]
    public async Task TryResume_NoActiveSession_PurgesStaleRows()
    {
        _persistence.GetActiveSessionAsync(_seedUserId, Arg.Any<CancellationToken>())
            .Returns((BillingSession?)null);

        await CreateSut().TryResumeAsync();

        await Task.Delay(100);

        await _persistence.Received(1)
            .PurgeStaleSessionsAsync(TimeSpan.FromDays(7), Arg.Any<CancellationToken>());
    }

    // ── Active session found → user resumes ────────────────────────

    [Fact]
    public async Task TryResume_UserResumes_ReturnsResumedWithSession()
    {
        var session = CreateActiveSession();
        _persistence.GetActiveSessionAsync(_seedUserId, Arg.Any<CancellationToken>())
            .Returns(session);
        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(true);

        var result = await CreateSut().TryResumeAsync();

        Assert.Equal(ResumeOutcome.Resumed, result.Outcome);
        Assert.NotNull(result.Session);
        Assert.Equal(session.SessionId, result.Session!.SessionId);
    }

    [Fact]
    public async Task TryResume_UserResumes_StartsBillingSession()
    {
        _persistence.GetActiveSessionAsync(_seedUserId, Arg.Any<CancellationToken>())
            .Returns(CreateActiveSession());
        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(true);

        await CreateSut().TryResumeAsync();

        await _billingSession.Received(1).StartSessionAsync();
    }

    [Fact]
    public async Task TryResume_UserResumes_DoesNotMarkCancelled()
    {
        var session = CreateActiveSession();
        _persistence.GetActiveSessionAsync(_seedUserId, Arg.Any<CancellationToken>())
            .Returns(session);
        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(true);

        await CreateSut().TryResumeAsync();

        await _persistence.DidNotReceive()
            .MarkCancelledAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ── Active session found → user discards ───────────────────────

    [Fact]
    public async Task TryResume_UserDiscards_ReturnsDiscarded()
    {
        var session = CreateActiveSession();
        _persistence.GetActiveSessionAsync(_seedUserId, Arg.Any<CancellationToken>())
            .Returns(session);
        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(false);

        var result = await CreateSut().TryResumeAsync();

        Assert.Equal(ResumeOutcome.Discarded, result.Outcome);
        Assert.NotNull(result.Session);
    }

    [Fact]
    public async Task TryResume_UserDiscards_MarksCancelled()
    {
        var session = CreateActiveSession();
        _persistence.GetActiveSessionAsync(_seedUserId, Arg.Any<CancellationToken>())
            .Returns(session);
        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(false);

        await CreateSut().TryResumeAsync();

        await _persistence.Received(1)
            .MarkCancelledAsync(session.SessionId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryResume_UserDiscards_DoesNotStartBillingSession()
    {
        _persistence.GetActiveSessionAsync(_seedUserId, Arg.Any<CancellationToken>())
            .Returns(CreateActiveSession());
        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(false);

        await CreateSut().TryResumeAsync();

        await _billingSession.DidNotReceive().StartSessionAsync();
    }

    [Fact]
    public async Task TryResume_UserDiscards_PurgesStaleRows()
    {
        _persistence.GetActiveSessionAsync(_seedUserId, Arg.Any<CancellationToken>())
            .Returns(CreateActiveSession());
        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(false);

        await CreateSut().TryResumeAsync();

        await Task.Delay(100);

        await _persistence.Received(1)
            .PurgeStaleSessionsAsync(TimeSpan.FromDays(7), Arg.Any<CancellationToken>());
    }

    // ── User credential not found ──────────────────────────────────

    [Fact]
    public async Task TryResume_NoUserCredential_ReturnsNoSession()
    {
        _session.CurrentUserType.Returns(UserType.Manager);

        var result = await CreateSut().TryResumeAsync();

        Assert.Equal(ResumeOutcome.NoSession, result.Outcome);
    }

    [Fact]
    public async Task TryResume_NoUserCredential_SkipsPersistenceLookup()
    {
        _session.CurrentUserType.Returns(UserType.Manager);

        await CreateSut().TryResumeAsync();

        await _persistence.DidNotReceive()
            .GetActiveSessionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // ── Dialog invocation ───────────────────────────────────────────

    [Fact]
    public async Task TryResume_ShowsResumeDialog_WithSessionAndUserType()
    {
        var session = CreateActiveSession();
        _persistence.GetActiveSessionAsync(_seedUserId, Arg.Any<CancellationToken>())
            .Returns(session);
        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(false);

        await CreateSut().TryResumeAsync();

        _dialog.Received(1).ShowResumeBillingDialog(
            Arg.Is<BillingSession>(s => s.SessionId == session.SessionId),
            UserType.Admin);
    }

    // ── Cart restore integration ───────────────────────────────────

    [Fact]
    public async Task TryResume_UserResumes_CallsRestoreService()
    {
        var session = CreateActiveSession();
        _persistence.GetActiveSessionAsync(_seedUserId, Arg.Any<CancellationToken>())
            .Returns(session);
        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(true);

        await CreateSut().TryResumeAsync();

        await _restoreService.Received(1)
            .RestoreAsync(Arg.Is<BillingSession>(s => s.SessionId == session.SessionId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryResume_UserResumes_ReturnsRestoredCart()
    {
        var session = CreateActiveSession();
        var restoredCart = new RestoredCart
        {
            SessionId = session.SessionId,
            Items = [new RestoredCartItem
            {
                ProductId = 1, ProductName = "Test", Quantity = 2,
                UnitPrice = 100m, TaxRate = 18m,
                LineTotal = new LineTotal(200m, 36m, 236m)
            }],
            Subtotal = 200m, TotalTax = 36m, GrandTotal = 236m
        };
        _persistence.GetActiveSessionAsync(_seedUserId, Arg.Any<CancellationToken>())
            .Returns(session);
        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(true);
        _restoreService.RestoreAsync(Arg.Any<BillingSession>(), Arg.Any<CancellationToken>())
            .Returns(restoredCart);

        var result = await CreateSut().TryResumeAsync();

        Assert.NotNull(result.RestoredCart);
        Assert.Single(result.RestoredCart!.Items);
        Assert.Equal(236m, result.RestoredCart.GrandTotal);
    }

    [Fact]
    public async Task TryResume_UserResumes_RestoreReturnsNull_StillResumed()
    {
        _persistence.GetActiveSessionAsync(_seedUserId, Arg.Any<CancellationToken>())
            .Returns(CreateActiveSession());
        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(true);
        _restoreService.RestoreAsync(Arg.Any<BillingSession>(), Arg.Any<CancellationToken>())
            .Returns((RestoredCart?)null);

        var result = await CreateSut().TryResumeAsync();

        Assert.Equal(ResumeOutcome.Resumed, result.Outcome);
        Assert.Null(result.RestoredCart);
    }

    [Fact]
    public async Task TryResume_UserDiscards_DoesNotCallRestoreService()
    {
        _persistence.GetActiveSessionAsync(_seedUserId, Arg.Any<CancellationToken>())
            .Returns(CreateActiveSession());
        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(false);

        await CreateSut().TryResumeAsync();

        await _restoreService.DidNotReceive()
            .RestoreAsync(Arg.Any<BillingSession>(), Arg.Any<CancellationToken>());
    }

    // ── Post-resume verification ───────────────────────────────────

    [Fact]
    public async Task TryResume_UserResumes_ModeAndFocusHealthy_IsFullyResumed()
    {
        _persistence.GetActiveSessionAsync(_seedUserId, Arg.Any<CancellationToken>())
            .Returns(CreateActiveSession());
        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(true);
        _modeService.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(true);

        var result = await CreateSut().TryResumeAsync();

        Assert.True(result.IsModeAndFocusLockActive);
        Assert.True(result.IsFullyResumed);
    }

    [Fact]
    public async Task TryResume_UserResumes_ModeNotBilling_VerificationFails()
    {
        _persistence.GetActiveSessionAsync(_seedUserId, Arg.Any<CancellationToken>())
            .Returns(CreateActiveSession());
        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(true);
        _modeService.CurrentMode.Returns(OperationalMode.Management);
        _focusLock.IsFocusLocked.Returns(true);

        var result = await CreateSut().TryResumeAsync();

        Assert.False(result.IsModeAndFocusLockActive);
        Assert.False(result.IsFullyResumed);
    }

    [Fact]
    public async Task TryResume_UserResumes_FocusNotLocked_VerificationFails()
    {
        _persistence.GetActiveSessionAsync(_seedUserId, Arg.Any<CancellationToken>())
            .Returns(CreateActiveSession());
        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(true);
        _modeService.CurrentMode.Returns(OperationalMode.Billing);
        _focusLock.IsFocusLocked.Returns(false);

        var result = await CreateSut().TryResumeAsync();

        Assert.False(result.IsModeAndFocusLockActive);
        Assert.False(result.IsFullyResumed);
    }

    // ── Helpers ────────────────────────────────────────────────────

    private BillingSession CreateActiveSession() => new()
    {
        Id = 42,
        SessionId = Guid.NewGuid(),
        UserId = _seedUserId,
        IsActive = true,
        SerializedBillData = """{"items":[{"productId":1,"productName":"Widget","quantity":2,"unitPrice":100,"taxRate":18,"isTaxInclusive":false}]}""",
        CreatedTime = new DateTime(2025, 6, 15, 9, 0, 0, DateTimeKind.Utc),
        LastUpdated = new DateTime(2025, 6, 15, 9, 30, 0, DateTimeKind.Utc)
    };

    public void Dispose()
    {
        using var db = new AppDbContext(_dbOptions);
        db.Database.EnsureDeleted();
    }
}
