using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Singleton implementation of <see cref="IPredictiveFocusService"/>.
/// <para>
/// Listens to:
/// <list type="bullet">
///   <item><see cref="INavigationService"/> — page changes.</item>
///   <item><see cref="IFocusLockService"/> — billing lock transitions.</item>
///   <item><see cref="IAppStateService"/> — operational mode changes.</item>
///   <item><see cref="IFlowStateEngine"/> — flow state transitions
///         (adjusts idle timeout, priority boost, and input guard bypass).</item>
/// </list>
/// Delegates all focus-target decisions to <see cref="IFocusRuleEngine"/>
/// so context-aware rules are evaluated consistently across all transitions.
/// </para>
///
/// <para><b>Flow-state adaptation (via <see cref="FlowFocusAdapter"/>):</b></para>
/// <code>
///   ┌──────────┬──────────────┬────────────────┬──────────────────────────┐
///   │ State    │ IdleTimeout  │ PriorityBoost  │ BypassInputGuard         │
///   ├──────────┼──────────────┼────────────────┼──────────────────────────┤
///   │ Calm     │ 600 ms       │    0           │ false (standard)         │
///   │ Focused  │ 400 ms       │    5           │ false (standard)         │
///   │ Flow     │ 200 ms       │   10           │ true  (aggressive)       │
///   └──────────┴──────────────┴────────────────┴──────────────────────────┘
/// </code>
/// </summary>
public sealed partial class PredictiveFocusService : ObservableObject, IPredictiveFocusService
{
    private readonly IFocusLockService _focusLock;
    private readonly IAppStateService _appState;
    private readonly INavigationService _navigation;
    private readonly IFocusRuleEngine _ruleEngine;
    private readonly Lazy<IFlowStateEngine> _flowStateEngine;
    private readonly IEventBus _eventBus;
    private readonly Dispatcher? _dispatcher;
    private readonly Lock _flowStateLock = new();

    /// <summary>Timer that resets <see cref="IsUserInputActive"/> after idle.</summary>
    private readonly System.Timers.Timer _idleTimer;
    private IFlowStateEngine? _resolvedFlowStateEngine;
    private bool _flowStateSubscribed;
    private bool _disposed;

    public PredictiveFocusService(
        IFocusLockService focusLock,
        IAppStateService appState,
        INavigationService navigation,
        IFocusRuleEngine ruleEngine,
        Lazy<IFlowStateEngine> flowStateEngine,
        IEventBus eventBus,
        Dispatcher? dispatcher = null)
    {
        _focusLock = focusLock;
        _appState = appState;
        _navigation = navigation;
        _ruleEngine = ruleEngine;
        _flowStateEngine = flowStateEngine;
        _eventBus = eventBus;
        _dispatcher = dispatcher ?? Application.Current?.Dispatcher;

        // Use Calm default for the initial timer — the lazy engine may
        // not be resolved yet (circular-dependency avoidance).
        _idleTimer = new System.Timers.Timer(FlowFocusAdapter.GetIdleTimeoutMs(FlowState.Calm))
        {
            AutoReset = false
        };
        _idleTimer.Elapsed += OnIdleTimerElapsed;

        _focusLock.PropertyChanged += OnFocusLockChanged;
        _appState.PropertyChanged += OnAppStateChanged;
        _navigation.PropertyChanged += OnNavigationChanged;
    }

    // ── Observable properties ────────────────────────────────────────

    [ObservableProperty]
    public partial FocusHint? CurrentHint { get; private set; }

    [ObservableProperty]
    public partial bool IsUserInputActive { get; private set; }

    // ── Public API ───────────────────────────────────────────────────

    public void SignalUserInput()
    {
        RunOnDispatcher(() =>
        {
            IsUserInputActive = true;
            _idleTimer.Stop();
            _idleTimer.Interval = FlowFocusAdapter.GetIdleTimeoutMs(GetCurrentFlowState());
            _idleTimer.Start();
        });
    }

    public void RequestFocus(string elementName, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementName);
        EmitHint(FocusHint.Named(elementName, reason, priority: 10));
    }

    public void RequestFirstInput(string reason)
    {
        EmitHint(FocusHint.FirstInput(reason, priority: 5));
    }

    // ── Reactive handlers ────────────────────────────────────────────

    private void OnFocusLockChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IFocusLockService.IsFocusLocked))
            return;

        if (IsInputSuppressed())
            return;

        if (_focusLock.IsFocusLocked)
        {
            // Billing lock acquired → evaluate rule engine for billing page
            var context = new FocusContext(
                OperationalMode.Billing,
                CurrentPageKey(),
                FocusContextType.Page);
            EmitHint(_ruleEngine.Evaluate(context, "BillingLockAcquired", basePriority: 20));
        }
        else
        {
            // Billing lock released → evaluate for the restored management page
            var context = new FocusContext(
                _appState.CurrentMode,
                CurrentPageKey(),
                FocusContextType.Page);
            EmitHint(_ruleEngine.Evaluate(context, "BillingLockReleased", basePriority: 15));
        }
    }

    private void OnAppStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IAppStateService.CurrentMode))
            return;

        if (IsInputSuppressed())
            return;

        // Mode changed without a focus lock transition (e.g., mode switch
        // from management to billing before the lock engages).
        if (!_focusLock.IsFocusLocked)
        {
            var context = new FocusContext(
                _appState.CurrentMode,
                CurrentPageKey(),
                FocusContextType.Page);
            EmitHint(_ruleEngine.Evaluate(context, "ModeChanged"));
        }
    }

    private void OnNavigationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(INavigationService.CurrentPageKey))
            return;

        if (IsInputSuppressed())
            return;

        var context = new FocusContext(
            _appState.CurrentMode,
            CurrentPageKey(),
            FocusContextType.Page);
        EmitHint(_ruleEngine.Evaluate(context, "PageNavigated"));
    }

    private void OnFlowStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IFlowStateEngine.CurrentState))
            return;

        // Update the idle timer interval to match the new flow state
        _idleTimer.Interval = FlowFocusAdapter.GetIdleTimeoutMs(GetCurrentFlowState());
    }

    // ── Hint emission ────────────────────────────────────────────────

    private void EmitHint(FocusHint hint)
    {
        RunOnDispatcher(() =>
        {
            // Apply flow-state priority boost
            var boost = FlowFocusAdapter.GetPriorityBoost(GetCurrentFlowState());
            if (boost > 0)
                hint = hint with { Priority = hint.Priority + boost };

            CurrentHint = hint;
            _ = _eventBus.PublishAsync(new FocusHintChangedEvent(hint));
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> when the user-input suppression should block
    /// hint emission. In <see cref="FlowState.Flow"/>, the guard is
    /// bypassed — aggressive auto-focus tracks rapid input.
    /// </summary>
    private bool IsInputSuppressed()
    {
        if (!IsUserInputActive)
            return false;

        // Flow state bypasses the input guard — the operator is in
        // rapid-fire mode and focus should chase them aggressively.
        return !FlowFocusAdapter.ShouldBypassInputGuard(GetCurrentFlowState());
    }

    private string CurrentPageKey() =>
        _navigation.CurrentPageKey ?? string.Empty;

    private FlowState GetCurrentFlowState()
    {
        EnsureFlowStateEngineSubscribed();
        return _resolvedFlowStateEngine?.CurrentState ?? FlowState.Calm;
    }

    private void EnsureFlowStateEngineSubscribed()
    {
        lock (_flowStateLock)
        {
            if (_disposed || _flowStateSubscribed)
                return;
        }

        IFlowStateEngine engine;
        try
        {
            engine = _flowStateEngine.Value;
        }
        catch (Exception)
        {
            // FlowStateEngine resolution failed — focus hints will
            // work without flow-state adaptation.
            return;
        }

        lock (_flowStateLock)
        {
            if (_disposed || _flowStateSubscribed)
                return;

            engine.PropertyChanged += OnFlowStateChanged;
            _resolvedFlowStateEngine = engine;
            _flowStateSubscribed = true;
            _idleTimer.Interval = FlowFocusAdapter.GetIdleTimeoutMs(engine.CurrentState);
        }
    }

    // ── Idle timer ───────────────────────────────────────────────────

    private void OnIdleTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        RunOnDispatcher(() => IsUserInputActive = false);
    }

    private void RunOnDispatcher(Action action)
    {
        if (_dispatcher is null
            || _dispatcher.HasShutdownStarted
            || _dispatcher.HasShutdownFinished)
        {
            action();
            return;
        }

        if (_dispatcher.CheckAccess())
        {
            action();
            return;
        }

        _dispatcher.Invoke(action);
    }

    // ── Cleanup ──────────────────────────────────────────────────────

    public void Dispose()
    {
        lock (_flowStateLock)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_flowStateSubscribed && _resolvedFlowStateEngine is not null)
                _resolvedFlowStateEngine.PropertyChanged -= OnFlowStateChanged;

            _flowStateSubscribed = false;
            _resolvedFlowStateEngine = null;
        }

        _idleTimer.Elapsed -= OnIdleTimerElapsed;
        _idleTimer.Dispose();
        _focusLock.PropertyChanged -= OnFocusLockChanged;
        _appState.PropertyChanged -= OnAppStateChanged;
        _navigation.PropertyChanged -= OnNavigationChanged;
    }
}
