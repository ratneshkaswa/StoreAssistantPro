using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Events;
using StoreAssistantPro.Modules.Billing.Services;

namespace StoreAssistantPro.Tests.Services;

/// <summary>
/// Tests for <see cref="BillingFocusCoordinator"/> — verifies that
/// billing lifecycle events produce the correct focus hints via
/// <see cref="IPredictiveFocusService"/>.
/// </summary>
public class BillingFocusCoordinatorTests
{
    private readonly IPredictiveFocusService _focusService = Substitute.For<IPredictiveFocusService>();
    private readonly IFocusLockService _focusLock = Substitute.For<IFocusLockService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    // Captured event handlers
    private Func<BillingSessionStartedEvent, Task>? _onSessionStarted;
    private Func<CartChangedEvent, Task>? _onCartChanged;
    private Func<PaymentStartedEvent, Task>? _onPaymentStarted;
    private Func<BillingSessionCompletedEvent, Task>? _onSessionCompleted;
    private Func<BillingSessionCancelledEvent, Task>? _onSessionCancelled;

    private BillingFocusCoordinator CreateSut()
    {
        _focusLock.IsFocusLocked.Returns(true);

        _eventBus.When(x => x.Subscribe(Arg.Any<Func<BillingSessionStartedEvent, Task>>()))
            .Do(ci => _onSessionStarted = ci.Arg<Func<BillingSessionStartedEvent, Task>>());
        _eventBus.When(x => x.Subscribe(Arg.Any<Func<CartChangedEvent, Task>>()))
            .Do(ci => _onCartChanged = ci.Arg<Func<CartChangedEvent, Task>>());
        _eventBus.When(x => x.Subscribe(Arg.Any<Func<PaymentStartedEvent, Task>>()))
            .Do(ci => _onPaymentStarted = ci.Arg<Func<PaymentStartedEvent, Task>>());
        _eventBus.When(x => x.Subscribe(Arg.Any<Func<BillingSessionCompletedEvent, Task>>()))
            .Do(ci => _onSessionCompleted = ci.Arg<Func<BillingSessionCompletedEvent, Task>>());
        _eventBus.When(x => x.Subscribe(Arg.Any<Func<BillingSessionCancelledEvent, Task>>()))
            .Do(ci => _onSessionCancelled = ci.Arg<Func<BillingSessionCancelledEvent, Task>>());

        return new BillingFocusCoordinator(_focusService, _focusLock, _eventBus);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Subscription wiring
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_SubscribesToAllBillingEvents()
    {
        var sut = CreateSut();

        _eventBus.Received(1).Subscribe(Arg.Any<Func<BillingSessionStartedEvent, Task>>());
        _eventBus.Received(1).Subscribe(Arg.Any<Func<CartChangedEvent, Task>>());
        _eventBus.Received(1).Subscribe(Arg.Any<Func<PaymentStartedEvent, Task>>());
        _eventBus.Received(1).Subscribe(Arg.Any<Func<BillingSessionCompletedEvent, Task>>());
        _eventBus.Received(1).Subscribe(Arg.Any<Func<BillingSessionCancelledEvent, Task>>());
    }

    [Fact]
    public void Dispose_UnsubscribesFromAllBillingEvents()
    {
        var sut = CreateSut();

        sut.Dispose();

        _eventBus.Received(1).Unsubscribe(Arg.Any<Func<BillingSessionStartedEvent, Task>>());
        _eventBus.Received(1).Unsubscribe(Arg.Any<Func<CartChangedEvent, Task>>());
        _eventBus.Received(1).Unsubscribe(Arg.Any<Func<PaymentStartedEvent, Task>>());
        _eventBus.Received(1).Unsubscribe(Arg.Any<Func<BillingSessionCompletedEvent, Task>>());
        _eventBus.Received(1).Unsubscribe(Arg.Any<Func<BillingSessionCancelledEvent, Task>>());
    }

    // ══════════════════════════════════════════════════════════════════
    //  Session started → focus BillingSearchBox
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SessionStarted_WhenLocked_FocusesBillingSearchBox()
    {
        var sut = CreateSut();
        _focusLock.IsFocusLocked.Returns(true);

        await _onSessionStarted!(new BillingSessionStartedEvent());

        _focusService.Received(1).RequestFocus(
            "BillingSearchBox",
            "BillingSessionStarted");
    }

    [Fact]
    public async Task SessionStarted_WhenNotLocked_DoesNotFocus()
    {
        var sut = CreateSut();
        _focusLock.IsFocusLocked.Returns(false);

        await _onSessionStarted!(new BillingSessionStartedEvent());

        _focusService.DidNotReceive().RequestFocus(
            Arg.Any<string>(), Arg.Any<string>());
    }

    // ══════════════════════════════════════════════════════════════════
    //  Cart changed (item added) → refocus BillingSearchBox
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CartChanged_WhenLocked_RefocusesBillingSearchBox()
    {
        var sut = CreateSut();
        _focusLock.IsFocusLocked.Returns(true);

        await _onCartChanged!(new CartChangedEvent(Guid.NewGuid(), "{}"));

        _focusService.Received(1).RequestFocus(
            "BillingSearchBox",
            "CartChanged");
    }

    [Fact]
    public async Task CartChanged_WhenNotLocked_DoesNotFocus()
    {
        var sut = CreateSut();
        _focusLock.IsFocusLocked.Returns(false);

        await _onCartChanged!(new CartChangedEvent(Guid.NewGuid(), "{}"));

        _focusService.DidNotReceive().RequestFocus(
            Arg.Any<string>(), Arg.Any<string>());
    }

    // ══════════════════════════════════════════════════════════════════
    //  Payment started → focus PaymentAmountInput
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PaymentStarted_WhenLocked_FocusesPaymentInput()
    {
        var sut = CreateSut();
        _focusLock.IsFocusLocked.Returns(true);

        await _onPaymentStarted!(new PaymentStartedEvent(Guid.NewGuid(), "{}"));

        _focusService.Received(1).RequestFocus(
            "PaymentAmountInput",
            "PaymentStarted");
    }

    [Fact]
    public async Task PaymentStarted_WhenNotLocked_DoesNotFocus()
    {
        var sut = CreateSut();
        _focusLock.IsFocusLocked.Returns(false);

        await _onPaymentStarted!(new PaymentStartedEvent(Guid.NewGuid(), "{}"));

        _focusService.DidNotReceive().RequestFocus(
            Arg.Any<string>(), Arg.Any<string>());
    }

    // ══════════════════════════════════════════════════════════════════
    //  Session completed → reset focus for next customer
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SessionCompleted_FocusesBillingSearchBox()
    {
        var sut = CreateSut();

        await _onSessionCompleted!(new BillingSessionCompletedEvent());

        // Always emits — even if lock is releasing, the hint is queued
        _focusService.Received(1).RequestFocus(
            "BillingSearchBox",
            "BillingSessionCompleted");
    }

    // ══════════════════════════════════════════════════════════════════
    //  Session cancelled → reset focus for fresh start
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SessionCancelled_FocusesBillingSearchBox()
    {
        var sut = CreateSut();

        await _onSessionCancelled!(new BillingSessionCancelledEvent());

        _focusService.Received(1).RequestFocus(
            "BillingSearchBox",
            "BillingSessionCancelled");
    }

    // ══════════════════════════════════════════════════════════════════
    //  Full billing cycle — end-to-end focus flow
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task FullBillingCycle_FocusFlowIsCorrect()
    {
        var sut = CreateSut();
        _focusLock.IsFocusLocked.Returns(true);
        _focusService.ClearReceivedCalls();

        // 1. Session starts → product search
        await _onSessionStarted!(new BillingSessionStartedEvent());
        _focusService.Received(1).RequestFocus(
            "BillingSearchBox",
            "BillingSessionStarted");

        // 2. First item scanned → back to product search
        _focusService.ClearReceivedCalls();
        await _onCartChanged!(new CartChangedEvent(Guid.NewGuid(), "{}"));
        _focusService.Received(1).RequestFocus(
            "BillingSearchBox",
            "CartChanged");

        // 3. Second item scanned → back to product search
        _focusService.ClearReceivedCalls();
        await _onCartChanged!(new CartChangedEvent(Guid.NewGuid(), "{}"));
        _focusService.Received(1).RequestFocus(
            "BillingSearchBox",
            "CartChanged");

        // 4. Payment initiated → payment amount input
        _focusService.ClearReceivedCalls();
        await _onPaymentStarted!(new PaymentStartedEvent(Guid.NewGuid(), "{}"));
        _focusService.Received(1).RequestFocus(
            "PaymentAmountInput",
            "PaymentStarted");

        // 5. Payment completed → ready for next customer
        _focusService.ClearReceivedCalls();
        await _onSessionCompleted!(new BillingSessionCompletedEvent());
        _focusService.Received(1).RequestFocus(
            "BillingSearchBox",
            "BillingSessionCompleted");
    }

    // ══════════════════════════════════════════════════════════════════
    //  Focus lock coordination
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MultipleFastScans_EachRefocusesSearchBox()
    {
        var sut = CreateSut();
        _focusLock.IsFocusLocked.Returns(true);
        _focusService.ClearReceivedCalls();

        // Simulate rapid barcode scanning
        for (var i = 0; i < 5; i++)
            await _onCartChanged!(new CartChangedEvent(Guid.NewGuid(), "{}"));

        _focusService.Received(5).RequestFocus(
            "BillingSearchBox",
            "CartChanged");
    }

    [Fact]
    public async Task CancelledSession_ResetsForFreshBill()
    {
        var sut = CreateSut();
        _focusLock.IsFocusLocked.Returns(true);

        // Start, add items, then cancel
        await _onSessionStarted!(new BillingSessionStartedEvent());
        await _onCartChanged!(new CartChangedEvent(Guid.NewGuid(), "{}"));

        _focusService.ClearReceivedCalls();
        await _onSessionCancelled!(new BillingSessionCancelledEvent());

        _focusService.Received(1).RequestFocus(
            "BillingSearchBox",
            "BillingSessionCancelled");
    }

    // ══════════════════════════════════════════════════════════════════
    //  Element name contract validation
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void BillingSearchBox_MatchesFocusRuleEngineConstant()
    {
        // Both FocusRuleEngine and BillingFocusCoordinator must
        // use the same element name for the billing search box.
        var coordinatorSrc = File.ReadAllText(
            Path.Combine(FindSolutionRoot(), "Modules", "Billing", "Services", "BillingFocusCoordinator.cs"));
        var ruleEngineSrc = File.ReadAllText(
            Path.Combine(FindSolutionRoot(), "Core", "Services", "FocusRuleEngine.cs"));

        Assert.Contains("\"BillingSearchBox\"", coordinatorSrc, StringComparison.Ordinal);
        Assert.Contains("\"BillingSearchBox\"", ruleEngineSrc, StringComparison.Ordinal);
    }

    [Fact]
    public void PaymentAmountInput_IsDeclared()
    {
        var src = File.ReadAllText(
            Path.Combine(FindSolutionRoot(), "Modules", "Billing", "Services", "BillingFocusCoordinator.cs"));

        Assert.Contains("\"PaymentAmountInput\"", src, StringComparison.Ordinal);
    }

    private static string FindSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0
                || Directory.GetFiles(dir, "*.slnx").Length > 0)
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new InvalidOperationException("Cannot find solution root.");
    }
}
