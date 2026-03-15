using NSubstitute;
using StoreAssistantPro.Core.Events;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Threading;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Services;

public class PredictiveFocusServiceTests
{
    private readonly IFocusLockService _focusLock = Substitute.For<IFocusLockService>();
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly IFlowStateEngine _flowStateEngine = Substitute.For<IFlowStateEngine>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    // Use a real FocusRuleEngine with an empty registry for backward-compat
    private readonly FocusMapRegistry _focusMapRegistry = new();

    private PredictiveFocusService CreateSut(Lazy<IFlowStateEngine>? flowStateEngine = null, Dispatcher? dispatcher = null)
    {
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _focusLock.IsFocusLocked.Returns(false);
        _navigation.CurrentPageKey.Returns(string.Empty);
        _flowStateEngine.CurrentState.Returns(FlowState.Calm);
        var ruleEngine = new FocusRuleEngine(_focusMapRegistry);
        return new PredictiveFocusService(
            _focusLock,
            _appState,
            _navigation,
            ruleEngine,
            flowStateEngine ?? new Lazy<IFlowStateEngine>(_flowStateEngine),
            _eventBus,
            dispatcher);
    }

    // ── Initial state ──────────────────────────────────────────────

    [Fact]
    public void InitialState_NoHint()
    {
        var sut = CreateSut();

        Assert.Null(sut.CurrentHint);
        Assert.False(sut.IsUserInputActive);
    }

    // ── Billing lock acquired ──────────────────────────────────────

    [Fact]
    public void BillingLockAcquired_EmitsNamedHint()
    {
        var sut = CreateSut();

        _focusLock.IsFocusLocked.Returns(true);
        _focusLock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _focusLock,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));

        Assert.NotNull(sut.CurrentHint);
        Assert.Equal(FocusStrategy.Named, sut.CurrentHint!.Strategy);
        Assert.Equal("BillingSearchBox", sut.CurrentHint.ElementName);
        Assert.Equal("BillingLockAcquired", sut.CurrentHint.Reason);
    }

    [Fact]
    public void BillingLockAcquired_PublishesEvent()
    {
        var sut = CreateSut();
        _eventBus.ClearReceivedCalls();

        _focusLock.IsFocusLocked.Returns(true);
        _focusLock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _focusLock,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));

        _eventBus.Received(1).PublishAsync(
            Arg.Is<FocusHintChangedEvent>(e =>
                e.Hint.Strategy == FocusStrategy.Named &&
                e.Hint.ElementName == "BillingSearchBox"));
    }

    // ── Billing lock released ──────────────────────────────────────

    [Fact]
    public void BillingLockReleased_EmitsFirstInputHint()
    {
        var sut = CreateSut();

        // First acquire
        _focusLock.IsFocusLocked.Returns(true);
        _focusLock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _focusLock,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));

        // Then release
        _focusLock.IsFocusLocked.Returns(false);
        _focusLock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _focusLock,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));

        Assert.NotNull(sut.CurrentHint);
        Assert.Equal(FocusStrategy.FirstInput, sut.CurrentHint!.Strategy);
        Assert.Equal("BillingLockReleased", sut.CurrentHint.Reason);
    }

    // ── Mode change ────────────────────────────────────────────────

    [Fact]
    public void ModeChanged_ToBilling_WhenUnlocked_EmitsBillingSearchBoxHint()
    {
        var sut = CreateSut();
        _eventBus.ClearReceivedCalls();

        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _appState,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IAppStateService.CurrentMode)));

        // Rule engine: Billing + Page → Named("BillingSearchBox")
        Assert.NotNull(sut.CurrentHint);
        Assert.Equal(FocusStrategy.Named, sut.CurrentHint!.Strategy);
        Assert.Equal("BillingSearchBox", sut.CurrentHint.ElementName);
        Assert.Equal("ModeChanged", sut.CurrentHint.Reason);
    }

    [Fact]
    public void ModeChanged_ToManagement_WhenUnlocked_EmitsFirstInputHint()
    {
        var sut = CreateSut();

        // First switch to billing
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _appState,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IAppStateService.CurrentMode)));

        _eventBus.ClearReceivedCalls();

        // Switch back to management (no map registered → FirstInput)
        _appState.CurrentMode.Returns(OperationalMode.Management);
        _appState.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _appState,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IAppStateService.CurrentMode)));

        Assert.NotNull(sut.CurrentHint);
        Assert.Equal(FocusStrategy.FirstInput, sut.CurrentHint!.Strategy);
        Assert.Equal("ModeChanged", sut.CurrentHint.Reason);
    }

    [Fact]
    public void ModeChanged_WhenLocked_DoesNotEmit()
    {
        var sut = CreateSut();

        // Lock first
        _focusLock.IsFocusLocked.Returns(true);
        _focusLock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _focusLock,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));

        _eventBus.ClearReceivedCalls();

        // Mode change while locked — no extra hint (lock already handled it)
        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _appState,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IAppStateService.CurrentMode)));

        _eventBus.DidNotReceive().PublishAsync(Arg.Any<FocusHintChangedEvent>());
    }

    // ── Irrelevant property changes ────────────────────────────────

    [Fact]
    public void IrrelevantPropertyChange_DoesNotEmit()
    {
        var sut = CreateSut();
        _eventBus.ClearReceivedCalls();

        _focusLock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _focusLock,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusLockService.ActiveModule)));

        _appState.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _appState,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IAppStateService.FirmName)));

        _eventBus.DidNotReceive().PublishAsync(Arg.Any<FocusHintChangedEvent>());
        Assert.Null(sut.CurrentHint);
    }

    // ── User input suppression ─────────────────────────────────────

    [Fact]
    public void SignalUserInput_SetsActiveFlag()
    {
        var sut = CreateSut();

        sut.SignalUserInput();

        Assert.True(sut.IsUserInputActive);
    }

    [Fact]
    public void WhenUserInputActive_LockTransitionSuppressed()
    {
        var sut = CreateSut();
        sut.SignalUserInput();
        _eventBus.ClearReceivedCalls();

        _focusLock.IsFocusLocked.Returns(true);
        _focusLock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _focusLock,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));

        // Hint should NOT be emitted while user is typing
        _eventBus.DidNotReceive().PublishAsync(Arg.Any<FocusHintChangedEvent>());
        Assert.Null(sut.CurrentHint);
    }

    [Fact]
    public void WhenUserInputActive_ModeChangeSuppressed()
    {
        var sut = CreateSut();
        sut.SignalUserInput();
        _eventBus.ClearReceivedCalls();

        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _appState,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IAppStateService.CurrentMode)));

        _eventBus.DidNotReceive().PublishAsync(Arg.Any<FocusHintChangedEvent>());
    }

    // ── RequestFocus ───────────────────────────────────────────────

    [Fact]
    public void RequestFocus_EmitsNamedHint()
    {
        var sut = CreateSut();

        sut.RequestFocus("CustomerNameBox", "AfterSave");

        Assert.NotNull(sut.CurrentHint);
        Assert.Equal(FocusStrategy.Named, sut.CurrentHint!.Strategy);
        Assert.Equal("CustomerNameBox", sut.CurrentHint.ElementName);
        Assert.Equal("AfterSave", sut.CurrentHint.Reason);
        Assert.Equal(10, sut.CurrentHint.Priority);
    }

    [Fact]
    public void RequestFocus_EmptyName_Throws()
    {
        var sut = CreateSut();

        Assert.Throws<ArgumentException>(() => sut.RequestFocus("", "test"));
    }

    // ── RequestFirstInput ──────────────────────────────────────────

    [Fact]
    public void RequestFirstInput_EmitsFirstInputHint()
    {
        var sut = CreateSut();

        sut.RequestFirstInput("PageLoaded");

        Assert.NotNull(sut.CurrentHint);
        Assert.Equal(FocusStrategy.FirstInput, sut.CurrentHint!.Strategy);
        Assert.Equal("PageLoaded", sut.CurrentHint.Reason);
        Assert.Equal(5, sut.CurrentHint.Priority);
    }

    // ── Hint priority ordering ─────────────────────────────────────

    [Fact]
    public void BillingLockHint_HasHigherPriority_ThanModeChange()
    {
        var sut = CreateSut();

        _focusLock.IsFocusLocked.Returns(true);
        _focusLock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _focusLock,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));

        Assert.True(sut.CurrentHint!.Priority > 5,
            "Billing lock hint should have higher priority than mode-change hints");
    }

    // ── PropertyChanged notifications ──────────────────────────────

    [Fact]
    public void CurrentHint_RaisesPropertyChanged()
    {
        var sut = CreateSut();
        var raised = false;
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IPredictiveFocusService.CurrentHint))
                raised = true;
        };

        sut.RequestFirstInput("test");

        Assert.True(raised);
    }

    [Fact]
    public void IsUserInputActive_RaisesPropertyChanged()
    {
        var sut = CreateSut();
        var raised = false;
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IPredictiveFocusService.IsUserInputActive))
                raised = true;
        };

        sut.SignalUserInput();

        Assert.True(raised);
    }

    [Fact]
    public void RequestFirstInput_FromBackgroundThread_Should_MarshalHintUpdateToDispatcher()
    {
        RunOnStaThread(() =>
        {
            var sut = CreateSut(dispatcher: Dispatcher.CurrentDispatcher);
            var dispatcherThreadId = Thread.CurrentThread.ManagedThreadId;
            var raisedThreadId = -1;

            sut.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IPredictiveFocusService.CurrentHint))
                    raisedThreadId = Thread.CurrentThread.ManagedThreadId;
            };

            var worker = new Thread(() => sut.RequestFirstInput("Background"));
            worker.Start();
            WaitForThread(worker);

            Assert.NotNull(sut.CurrentHint);
            Assert.Equal(dispatcherThreadId, raisedThreadId);
        });
    }

    [Fact]
    public void IdleTimerElapsed_FromBackgroundThread_Should_MarshalInputResetToDispatcher()
    {
        RunOnStaThread(() =>
        {
            var sut = CreateSut(dispatcher: Dispatcher.CurrentDispatcher);
            sut.SignalUserInput();
            Assert.True(sut.IsUserInputActive);

            var dispatcherThreadId = Thread.CurrentThread.ManagedThreadId;
            var raisedThreadId = -1;

            sut.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IPredictiveFocusService.IsUserInputActive))
                    raisedThreadId = Thread.CurrentThread.ManagedThreadId;
            };

            var idleCallback = typeof(PredictiveFocusService).GetMethod(
                "OnIdleTimerElapsed",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(idleCallback);

            var worker = new Thread(() => idleCallback!.Invoke(sut, [null, null!]));
            worker.Start();
            WaitForThread(worker);

            Assert.False(sut.IsUserInputActive);
            Assert.Equal(dispatcherThreadId, raisedThreadId);
        });
    }

    // ── FocusHint model tests ──────────────────────────────────────

    [Fact]
    public void FocusHint_FirstInput_Factory()
    {
        var hint = FocusHint.FirstInput("test", 3);

        Assert.Equal(FocusStrategy.FirstInput, hint.Strategy);
        Assert.Equal(string.Empty, hint.ElementName);
        Assert.Equal("test", hint.Reason);
        Assert.Equal(3, hint.Priority);
    }

    [Fact]
    public void FocusHint_Named_Factory()
    {
        var hint = FocusHint.Named("SearchBox", "nav", 7);

        Assert.Equal(FocusStrategy.Named, hint.Strategy);
        Assert.Equal("SearchBox", hint.ElementName);
        Assert.Equal("nav", hint.Reason);
        Assert.Equal(7, hint.Priority);
    }

    [Fact]
    public void FocusHint_Preserve_Factory()
    {
        var hint = FocusHint.Preserve("userTyping");

        Assert.Equal(FocusStrategy.Preserve, hint.Strategy);
        Assert.Equal(string.Empty, hint.ElementName);
        Assert.Equal("userTyping", hint.Reason);
        Assert.Equal(0, hint.Priority);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Flow-state integration tests
    // ═══════════════════════════════════════════════════════════════

    // ── Priority boost ─────────────────────────────────────────────

    [Fact]
    public void FlowState_Calm_NoPriorityBoost()
    {
        _flowStateEngine.CurrentState.Returns(FlowState.Calm);
        var sut = CreateSut();

        sut.RequestFirstInput("test");

        Assert.Equal(5, sut.CurrentHint!.Priority); // base 5 + boost 0
    }

    [Fact]
    public void FlowState_Focused_PriorityBoostedBy5()
    {
        var sut = CreateSut();
        _flowStateEngine.CurrentState.Returns(FlowState.Focused);

        sut.RequestFirstInput("test");

        Assert.Equal(10, sut.CurrentHint!.Priority); // base 5 + boost 5
    }

    [Fact]
    public void FlowState_Flow_PriorityBoostedBy10()
    {
        var sut = CreateSut();
        _flowStateEngine.CurrentState.Returns(FlowState.Flow);

        sut.RequestFirstInput("test");

        Assert.Equal(15, sut.CurrentHint!.Priority); // base 5 + boost 10
    }

    [Fact]
    public void FlowState_Flow_RequestFocus_PriorityBoostedBy10()
    {
        var sut = CreateSut();
        _flowStateEngine.CurrentState.Returns(FlowState.Flow);

        sut.RequestFocus("SearchBox", "test");

        Assert.Equal(20, sut.CurrentHint!.Priority); // base 10 + boost 10
    }

    // ── Input guard bypass ─────────────────────────────────────────

    [Fact]
    public void FlowState_Flow_BypassesInputGuard_ForBillingLock()
    {
        var sut = CreateSut();
        sut.SignalUserInput();
        Assert.True(sut.IsUserInputActive);

        // Switch to Flow — input guard should be bypassed
        _flowStateEngine.CurrentState.Returns(FlowState.Flow);
        _eventBus.ClearReceivedCalls();

        _focusLock.IsFocusLocked.Returns(true);
        _focusLock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _focusLock,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));

        // In Flow, hints should be emitted even while user input is active
        Assert.NotNull(sut.CurrentHint);
        Assert.Equal("BillingLockAcquired", sut.CurrentHint!.Reason);
    }

    [Fact]
    public void FlowState_Flow_BypassesInputGuard_ForNavigation()
    {
        var sut = CreateSut();
        sut.SignalUserInput();
        _flowStateEngine.CurrentState.Returns(FlowState.Flow);
        _eventBus.ClearReceivedCalls();

        _navigation.CurrentPageKey.Returns("ProductsView");
        _navigation.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _navigation,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(INavigationService.CurrentPageKey)));

        Assert.NotNull(sut.CurrentHint);
        Assert.Equal("PageNavigated", sut.CurrentHint!.Reason);
    }

    [Fact]
    public void FlowState_Flow_BypassesInputGuard_ForModeChange()
    {
        var sut = CreateSut();
        sut.SignalUserInput();
        _flowStateEngine.CurrentState.Returns(FlowState.Flow);
        _eventBus.ClearReceivedCalls();

        _appState.CurrentMode.Returns(OperationalMode.Billing);
        _appState.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _appState,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IAppStateService.CurrentMode)));

        Assert.NotNull(sut.CurrentHint);
        Assert.Equal("ModeChanged", sut.CurrentHint!.Reason);
    }

    [Fact]
    public void FlowState_Focused_DoesNotBypassInputGuard()
    {
        var sut = CreateSut();
        sut.SignalUserInput();
        _flowStateEngine.CurrentState.Returns(FlowState.Focused);
        _eventBus.ClearReceivedCalls();

        _focusLock.IsFocusLocked.Returns(true);
        _focusLock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _focusLock,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));

        // Focused does NOT bypass the guard
        _eventBus.DidNotReceive().PublishAsync(Arg.Any<FocusHintChangedEvent>());
    }

    [Fact]
    public void FlowState_Calm_DoesNotBypassInputGuard()
    {
        var sut = CreateSut();
        sut.SignalUserInput();
        _flowStateEngine.CurrentState.Returns(FlowState.Calm);
        _eventBus.ClearReceivedCalls();

        _focusLock.IsFocusLocked.Returns(true);
        _focusLock.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _focusLock,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusLockService.IsFocusLocked)));

        _eventBus.DidNotReceive().PublishAsync(Arg.Any<FocusHintChangedEvent>());
    }

    // ── Flow state change → idle timer update ──────────────────────

    [Fact]
    public void FlowStateChange_IrrelevantProperty_DoesNothing()
    {
        var sut = CreateSut();
        _eventBus.ClearReceivedCalls();

        _flowStateEngine.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _flowStateEngine,
            new System.ComponentModel.PropertyChangedEventArgs("SomeOtherProperty"));

        // Should not emit any hints
        _eventBus.DidNotReceive().PublishAsync(Arg.Any<FocusHintChangedEvent>());
    }

    // ── Published event includes boosted priority ──────────────────

    [Fact]
    public void FlowState_Flow_PublishedEvent_HasBoostedPriority()
    {
        var sut = CreateSut();
        _flowStateEngine.CurrentState.Returns(FlowState.Flow);
        _eventBus.ClearReceivedCalls();

        sut.RequestFirstInput("test");

        _eventBus.Received(1).PublishAsync(
            Arg.Is<FocusHintChangedEvent>(e => e.Hint.Priority == 15)); // 5 + 10
    }

    [Fact]
    public async Task Dispose_BeforeLazyFlowEngineResolves_DoesNotAttachLateSubscription()
    {
        using var resolutionStarted = new ManualResetEventSlim(false);
        using var releaseResolution = new ManualResetEventSlim(false);
        var blockingEngine = new TestFlowStateEngine();
        var lazyEngine = new Lazy<IFlowStateEngine>(() =>
        {
            resolutionStarted.Set();
            releaseResolution.Wait(TimeSpan.FromSeconds(5));
            return blockingEngine;
        });

        var sut = CreateSut(lazyEngine);
        var pendingRequest = Task.Run(() => sut.RequestFirstInput("test"));

        Assert.True(resolutionStarted.Wait(TimeSpan.FromSeconds(5)));

        sut.Dispose();
        releaseResolution.Set();

        await pendingRequest.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(0, blockingEngine.SubscribeCount);
        Assert.Equal(0, blockingEngine.UnsubscribeCount);
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? failure = null;
        using var completed = new ManualResetEventSlim(false);

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
                completed.Set();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.True(completed.Wait(TimeSpan.FromSeconds(10)), "STA test thread timed out.");

        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static void WaitForThread(Thread thread)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (thread.IsAlive && DateTime.UtcNow < deadline)
            DrainDispatcher();

        Assert.False(thread.IsAlive, "Worker thread timed out.");
    }

    private static void DrainDispatcher()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new DispatcherOperationCallback(_ =>
            {
                frame.Continue = false;
                return null;
            }),
            null);
        Dispatcher.PushFrame(frame);
    }

    private sealed class TestFlowStateEngine : IFlowStateEngine
    {
        private System.ComponentModel.PropertyChangedEventHandler? _propertyChanged;

        public int SubscribeCount { get; private set; }

        public int UnsubscribeCount { get; private set; }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged
        {
            add
            {
                SubscribeCount++;
                _propertyChanged += value;
            }
            remove
            {
                UnsubscribeCount++;
                _propertyChanged -= value;
            }
        }

        public FlowState CurrentState => FlowState.Calm;

        public string TransitionReason => string.Empty;

        public DateTime LastTransitionTime => DateTime.UtcNow;

        public bool IsInFlow => false;

        public void Dispose()
        {
        }

        public void Recompute()
        {
        }
    }
}
