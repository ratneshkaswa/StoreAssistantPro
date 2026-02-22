using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Modules.Billing.Events;
using StoreAssistantPro.Modules.Billing.Services;

namespace StoreAssistantPro.Tests.Services;

public class BillingAutoSaveServiceTests : IDisposable
{
    private readonly IBillingSessionPersistenceService _persistence =
        Substitute.For<IBillingSessionPersistenceService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    /// <summary>Short debounce for fast tests.</summary>
    private static readonly TimeSpan TestDebounce = TimeSpan.FromMilliseconds(50);

    /// <summary>Wait long enough for the debounce timer to fire.</summary>
    private static readonly TimeSpan WaitForDebounce = TimeSpan.FromMilliseconds(200);

    private Func<BillingSessionStartedEvent, Task>? _startedHandler;
    private Func<CartChangedEvent, Task>? _cartHandler;
    private Func<PaymentStartedEvent, Task>? _paymentHandler;
    private Func<BillingSessionCompletedEvent, Task>? _completedHandler;
    private Func<BillingSessionCancelledEvent, Task>? _cancelledHandler;

    private BillingAutoSaveService _sut = null!;

    private BillingAutoSaveService CreateSut()
    {
        _eventBus.When(e => e.Subscribe(Arg.Any<Func<BillingSessionStartedEvent, Task>>()))
            .Do(ci => _startedHandler = ci.Arg<Func<BillingSessionStartedEvent, Task>>());
        _eventBus.When(e => e.Subscribe(Arg.Any<Func<CartChangedEvent, Task>>()))
            .Do(ci => _cartHandler = ci.Arg<Func<CartChangedEvent, Task>>());
        _eventBus.When(e => e.Subscribe(Arg.Any<Func<PaymentStartedEvent, Task>>()))
            .Do(ci => _paymentHandler = ci.Arg<Func<PaymentStartedEvent, Task>>());
        _eventBus.When(e => e.Subscribe(Arg.Any<Func<BillingSessionCompletedEvent, Task>>()))
            .Do(ci => _completedHandler = ci.Arg<Func<BillingSessionCompletedEvent, Task>>());
        _eventBus.When(e => e.Subscribe(Arg.Any<Func<BillingSessionCancelledEvent, Task>>()))
            .Do(ci => _cancelledHandler = ci.Arg<Func<BillingSessionCancelledEvent, Task>>());

        _sut = new BillingAutoSaveService(
            _persistence, _eventBus,
            NullLogger<BillingAutoSaveService>.Instance,
            TestDebounce);

        return _sut;
    }

    // ── Subscription ───────────────────────────────────────────────

    [Fact]
    public void Constructor_SubscribesToAllFiveEvents()
    {
        _ = CreateSut();

        _eventBus.Received(1).Subscribe(Arg.Any<Func<BillingSessionStartedEvent, Task>>());
        _eventBus.Received(1).Subscribe(Arg.Any<Func<CartChangedEvent, Task>>());
        _eventBus.Received(1).Subscribe(Arg.Any<Func<PaymentStartedEvent, Task>>());
        _eventBus.Received(1).Subscribe(Arg.Any<Func<BillingSessionCompletedEvent, Task>>());
        _eventBus.Received(1).Subscribe(Arg.Any<Func<BillingSessionCancelledEvent, Task>>());
    }

    // ── Cart changed → debounced save ──────────────────────────────

    [Fact]
    public async Task CartChanged_SavesAfterDebounce()
    {
        _ = CreateSut();
        var sessionId = Guid.NewGuid();

        await _cartHandler!(new CartChangedEvent(sessionId, """{"v":1}"""));

        // Not saved yet — within debounce window
        await _persistence.DidNotReceive()
            .UpdateCartAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        // Wait for debounce to fire
        await Task.Delay(WaitForDebounce);

        await _persistence.Received(1)
            .UpdateCartAsync(sessionId, """{"v":1}""", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CartChanged_MultipleRapidChanges_OnlySavesLatest()
    {
        _ = CreateSut();
        var sessionId = Guid.NewGuid();

        await _cartHandler!(new CartChangedEvent(sessionId, """{"v":1}"""));
        await _cartHandler!(new CartChangedEvent(sessionId, """{"v":2}"""));
        await _cartHandler!(new CartChangedEvent(sessionId, """{"v":3}"""));

        await Task.Delay(WaitForDebounce);

        // Only the latest snapshot should have been saved
        await _persistence.Received(1)
            .UpdateCartAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _persistence.Received(1)
            .UpdateCartAsync(sessionId, """{"v":3}""", Arg.Any<CancellationToken>());
    }

    // ── Payment started → immediate flush ──────────────────────────

    [Fact]
    public async Task PaymentStarted_FlushesImmediately()
    {
        _ = CreateSut();
        var sessionId = Guid.NewGuid();

        await _paymentHandler!(new PaymentStartedEvent(sessionId, """{"pay":true}"""));

        // Saved immediately — no debounce wait
        await _persistence.Received(1)
            .UpdateCartAsync(sessionId, """{"pay":true}""", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PaymentStarted_CancelsPendingDebounce()
    {
        _ = CreateSut();
        var sessionId = Guid.NewGuid();

        // Queue a debounced save
        await _cartHandler!(new CartChangedEvent(sessionId, """{"v":1}"""));

        // Payment starts before debounce fires — immediate flush with payment data
        await _paymentHandler!(new PaymentStartedEvent(sessionId, """{"v":1,"pay":true}"""));

        await _persistence.Received(1)
            .UpdateCartAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        // Wait past debounce window — no second save should fire
        await Task.Delay(WaitForDebounce);

        await _persistence.Received(1)
            .UpdateCartAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Session completed → marks inactive ─────────────────────────

    [Fact]
    public async Task SessionCompleted_MarksCompleted()
    {
        _ = CreateSut();
        var sessionId = Guid.NewGuid();

        // Set up session context
        await _cartHandler!(new CartChangedEvent(sessionId, """{"v":1}"""));
        await _completedHandler!(new BillingSessionCompletedEvent());

        await _persistence.Received(1)
            .MarkCompletedAsync(sessionId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SessionCompleted_CancelsPendingDebounce()
    {
        _ = CreateSut();
        var sessionId = Guid.NewGuid();

        await _cartHandler!(new CartChangedEvent(sessionId, """{"v":1}"""));
        await _completedHandler!(new BillingSessionCompletedEvent());

        // Wait past debounce — the cart save should not fire
        await Task.Delay(WaitForDebounce);

        await _persistence.DidNotReceive()
            .UpdateCartAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Session cancelled → marks cancelled ────────────────────────

    [Fact]
    public async Task SessionCancelled_MarksCancelled()
    {
        _ = CreateSut();
        var sessionId = Guid.NewGuid();

        await _cartHandler!(new CartChangedEvent(sessionId, """{"v":1}"""));
        await _cancelledHandler!(new BillingSessionCancelledEvent());

        await _persistence.Received(1)
            .MarkCancelledAsync(sessionId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SessionCancelled_CancelsPendingDebounce()
    {
        _ = CreateSut();
        var sessionId = Guid.NewGuid();

        await _cartHandler!(new CartChangedEvent(sessionId, """{"v":1}"""));
        await _cancelledHandler!(new BillingSessionCancelledEvent());

        await Task.Delay(WaitForDebounce);

        await _persistence.DidNotReceive()
            .UpdateCartAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Session started → resets state ─────────────────────────────

    [Fact]
    public async Task SessionStarted_CancelsPendingDebounceFromPreviousSession()
    {
        _ = CreateSut();
        var oldSession = Guid.NewGuid();

        await _cartHandler!(new CartChangedEvent(oldSession, """{"old":true}"""));
        await _startedHandler!(new BillingSessionStartedEvent());

        // Wait past debounce — old session data should not save
        await Task.Delay(WaitForDebounce);

        await _persistence.DidNotReceive()
            .UpdateCartAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── IsSaving ───────────────────────────────────────────────────

    [Fact]
    public void IsSaving_InitiallyFalse()
    {
        _ = CreateSut();

        Assert.False(_sut.IsSaving);
    }

    // ── DebounceDelay ──────────────────────────────────────────────

    [Fact]
    public void DebounceDelay_ReturnsConfiguredValue()
    {
        _ = CreateSut();

        Assert.Equal(TestDebounce, _sut.DebounceDelay);
    }

    // ── Persistence failure → no throw ─────────────────────────────

    [Fact]
    public async Task CartChanged_PersistenceThrows_DoesNotBubble()
    {
        _ = CreateSut();
        var sessionId = Guid.NewGuid();

        _persistence.UpdateCartAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("DB error")));

        await _cartHandler!(new CartChangedEvent(sessionId, "{}"));
        await Task.Delay(WaitForDebounce);

        // Should not throw — error is logged
        Assert.False(_sut.IsSaving);
    }

    [Fact]
    public async Task SessionCompleted_PersistenceThrows_DoesNotBubble()
    {
        _ = CreateSut();
        var sessionId = Guid.NewGuid();

        _persistence.MarkCompletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("DB error")));

        await _cartHandler!(new CartChangedEvent(sessionId, "{}"));

        var ex = await Record.ExceptionAsync(
            () => _completedHandler!(new BillingSessionCompletedEvent()));

        Assert.Null(ex);
    }

    // ── Dispose ────────────────────────────────────────────────────

    [Fact]
    public void Dispose_UnsubscribesFromAllEvents()
    {
        var sut = CreateSut();

        sut.Dispose();

        _eventBus.Received(1).Unsubscribe(Arg.Any<Func<BillingSessionStartedEvent, Task>>());
        _eventBus.Received(1).Unsubscribe(Arg.Any<Func<CartChangedEvent, Task>>());
        _eventBus.Received(1).Unsubscribe(Arg.Any<Func<PaymentStartedEvent, Task>>());
        _eventBus.Received(1).Unsubscribe(Arg.Any<Func<BillingSessionCompletedEvent, Task>>());
        _eventBus.Received(1).Unsubscribe(Arg.Any<Func<BillingSessionCancelledEvent, Task>>());
    }

    // ── Test cleanup ───────────────────────────────────────────────

    public void Dispose() => _sut?.Dispose();
}
