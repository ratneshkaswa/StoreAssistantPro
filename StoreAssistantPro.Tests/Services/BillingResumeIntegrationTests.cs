using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Services;

namespace StoreAssistantPro.Tests.Services;

/// <summary>
/// Integration tests that verify the full resume → event chain →
/// mode switch → focus lock pipeline using real service implementations
/// wired through a real <see cref="EventBus"/>.
/// <para>
/// Only <see cref="IDbContextFactory{TContext}"/>,
/// <see cref="IDialogService"/>, <see cref="ISessionService"/>,
/// <see cref="IAppStateService"/>, and <see cref="IBillingSessionRestoreService"/>
/// are mocked — everything else is the production implementation.
/// </para>
/// </summary>
public class BillingResumeIntegrationTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly ISessionService _session = Substitute.For<ISessionService>();
    private readonly IDialogService _dialog = Substitute.For<IDialogService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

    // Real services (production implementations)
    private readonly EventBus _eventBus = new();
    private readonly BillingModeService _modeService;
    private readonly BillingSessionService _sessionService;
    private readonly FocusLockService _focusLock;
    private readonly SmartBillingModeService _smartMode;
    private readonly IBillingSessionPersistenceService _persistence;
    private readonly IBillingSessionRestoreService _restoreService =
        Substitute.For<IBillingSessionRestoreService>();
    private readonly IFeatureToggleService _featureToggle =
        Substitute.For<IFeatureToggleService>();

    private int _seedUserId;
    private readonly DateTime _now = new(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

    public BillingResumeIntegrationTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _session.CurrentUserType.Returns(UserType.Admin);
        _regional.Now.Returns(_now);

        // Track mode in the mock AppState
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _appState.CurrentBillingSession.Returns(BillingSessionState.None);
        _appState.When(a => a.SetMode(Arg.Any<OperationalMode>()))
            .Do(ci => _appState.CurrentMode.Returns(ci.Arg<OperationalMode>()));
        _appState.When(a => a.SetBillingSession(Arg.Any<BillingSessionState>()))
            .Do(ci => _appState.CurrentBillingSession.Returns(ci.Arg<BillingSessionState>()));

        // Real BillingModeService
        _modeService = new BillingModeService(_appState, _featureToggle, _eventBus);

        // Real BillingSessionService
        _sessionService = new BillingSessionService(_appState, _eventBus);

        // Real FocusLockService — subscribes to OperationalModeChangedEvent
        _focusLock = new FocusLockService(_eventBus);

        // Real SmartBillingModeService — subscribes to BillingSession events
        _smartMode = new SmartBillingModeService(
            _modeService, _sessionService, _focusLock, _eventBus);

        // Real persistence (uses InMemory DB)
        _persistence = new BillingSessionPersistenceService(
            CreateFactory(), _session, _regional, _perf,
            NullLogger<BillingSessionPersistenceService>.Instance);

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
        new(CreateFactory(), _session, _persistence, _sessionService,
            _restoreService, _modeService, _focusLock, _dialog, _perf,
            NullLogger<BillingResumeService>.Instance);

    // ── Full chain: Resume → Mode Switch → Focus Lock ──────────────

    [Fact]
    public async Task FullChain_Resume_SwitchesToBillingMode()
    {
        // Seed an active persisted session
        var sessionId = Guid.NewGuid();
        await _persistence.CreateAsync(sessionId, """{"items":[]}""");

        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(true);

        Assert.Equal(OperationalMode.Management, _modeService.CurrentMode);

        await CreateSut().TryResumeAsync();

        Assert.Equal(OperationalMode.Billing, _modeService.CurrentMode);
    }

    [Fact]
    public async Task FullChain_Resume_AcquiresFocusLock()
    {
        var sessionId = Guid.NewGuid();
        await _persistence.CreateAsync(sessionId, """{"items":[]}""");

        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(true);

        Assert.False(_focusLock.IsFocusLocked);

        await CreateSut().TryResumeAsync();

        Assert.True(_focusLock.IsFocusLocked);
        Assert.Equal("Billing", _focusLock.ActiveModule);
    }

    [Fact]
    public async Task FullChain_Resume_SessionStateIsActive()
    {
        var sessionId = Guid.NewGuid();
        await _persistence.CreateAsync(sessionId, """{"items":[]}""");

        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(true);

        Assert.Equal(BillingSessionState.None, _sessionService.CurrentState);

        await CreateSut().TryResumeAsync();

        Assert.Equal(BillingSessionState.Active, _sessionService.CurrentState);
    }

    [Fact]
    public async Task FullChain_Resume_ResultReportsHealthyIntegration()
    {
        var sessionId = Guid.NewGuid();
        await _persistence.CreateAsync(sessionId, """{"items":[]}""");

        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(true);

        var result = await CreateSut().TryResumeAsync();

        Assert.Equal(ResumeOutcome.Resumed, result.Outcome);
        Assert.True(result.IsModeAndFocusLockActive);
        Assert.True(result.IsFullyResumed);
    }

    [Fact]
    public async Task FullChain_Resume_CallsRestoreService()
    {
        var sessionId = Guid.NewGuid();
        await _persistence.CreateAsync(sessionId, """{"items":[{"productId":1}]}""");

        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(true);

        await CreateSut().TryResumeAsync();

        await _restoreService.Received(1)
            .RestoreAsync(Arg.Is<BillingSession>(s => s.SessionId == sessionId),
                Arg.Any<CancellationToken>());
    }

    // ── Full chain: Discard → No mode change ───────────────────────

    [Fact]
    public async Task FullChain_Discard_StaysInManagementMode()
    {
        var sessionId = Guid.NewGuid();
        await _persistence.CreateAsync(sessionId, """{"items":[]}""");

        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(false);

        await CreateSut().TryResumeAsync();

        Assert.Equal(OperationalMode.Management, _modeService.CurrentMode);
    }

    [Fact]
    public async Task FullChain_Discard_FocusLockNotAcquired()
    {
        var sessionId = Guid.NewGuid();
        await _persistence.CreateAsync(sessionId, """{"items":[]}""");

        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(false);

        await CreateSut().TryResumeAsync();

        Assert.False(_focusLock.IsFocusLocked);
    }

    [Fact]
    public async Task FullChain_Discard_SessionRemainsNone()
    {
        var sessionId = Guid.NewGuid();
        await _persistence.CreateAsync(sessionId, """{"items":[]}""");

        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(false);

        await CreateSut().TryResumeAsync();

        Assert.Equal(BillingSessionState.None, _sessionService.CurrentState);
    }

    [Fact]
    public async Task FullChain_Discard_PersistsAsCancelled()
    {
        var sessionId = Guid.NewGuid();
        await _persistence.CreateAsync(sessionId, """{"items":[]}""");

        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(false);

        await CreateSut().TryResumeAsync();

        // Verify the DB row is now inactive
        var stillActive = await _persistence.GetActiveSessionAsync(_seedUserId);
        Assert.Null(stillActive);
    }

    // ── Full chain: No session → unchanged state ───────────────────

    [Fact]
    public async Task FullChain_NoSession_AllServicesUnchanged()
    {
        // No session seeded

        var result = await CreateSut().TryResumeAsync();

        Assert.Equal(ResumeOutcome.NoSession, result.Outcome);
        Assert.Equal(OperationalMode.Management, _modeService.CurrentMode);
        Assert.False(_focusLock.IsFocusLocked);
        Assert.Equal(BillingSessionState.None, _sessionService.CurrentState);
    }

    // ── Navigation blocking ────────────────────────────────────────

    [Fact]
    public async Task FullChain_Resume_NavigationBlockedByFocusLock()
    {
        var sessionId = Guid.NewGuid();
        await _persistence.CreateAsync(sessionId, """{"items":[]}""");

        _dialog.ShowResumeBillingDialog(Arg.Any<BillingSession>(), Arg.Any<UserType>()).Returns(true);

        await CreateSut().TryResumeAsync();

        // After resume, attempting to acquire focus for another module should throw
        Assert.True(_focusLock.IsFocusLocked);
        Assert.Equal("Billing", _focusLock.ActiveModule);

        var ex = Assert.Throws<InvalidOperationException>(
            () => _focusLock.Acquire("Products"));

        Assert.Contains("Billing", ex.Message);
    }

    // ── Cleanup ────────────────────────────────────────────────────

    public void Dispose()
    {
        _smartMode.Dispose();
        _focusLock.Dispose();
        using var db = new AppDbContext(_dbOptions);
        db.Database.EnsureDeleted();
    }
}
