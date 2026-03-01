using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Events;

namespace StoreAssistantPro.Core.Workflows;

/// <summary>
/// Singleton implementation of <see cref="IZeroClickWorkflowService"/>.
/// <para>
/// Subscribes to key domain events and re-evaluates all registered rules
/// after each event. Only rules that return <see cref="ZeroClickConfidence.High"/>
/// and pass all safety gates are auto-executed.
/// </para>
/// <para>
/// <b>Flow-state adaptation (via <see cref="FlowZeroClickAdapter"/>):</b>
/// </para>
/// <code>
///   ┌──────────┬───────────────────┬──────────────────┬──────────────────────┐
///   │ State    │ AcceptMedium      │ AllowDataEntry   │ Effect               │
///   ├──────────┼───────────────────┼──────────────────┼──────────────────────┤
///   │ Calm     │ false             │ false            │ Conservative.        │
///   │ Focused  │ false             │ true             │ Standard.            │
///   │ Flow     │ true              │ true             │ Faster execution.    │
///   └──────────┴───────────────────┴──────────────────┴──────────────────────┘
/// </code>
/// <para>
/// <b>Safety invariant:</b> Destructive categories (Delete, SettingsChange,
/// FinancialConfirmation, SecuritySensitive) are <b>never</b> auto-executed
/// regardless of flow state.
/// </para>
/// </summary>
public sealed class ZeroClickWorkflowService : IZeroClickWorkflowService
{
    private readonly IEventBus _eventBus;
    private readonly IAppStateService _appState;
    private readonly IFocusLockService _focusLock;
    private readonly IWorkflowManager _workflowManager;
    private readonly IZeroClickSafetyPolicy _safetyPolicy;
    private readonly IFlowStateEngine _flowStateEngine;
    private readonly IPerformanceMonitor _perf;
    private readonly IRegionalSettingsService _regional;
    private readonly ILogger<ZeroClickWorkflowService> _logger;

    private readonly List<IZeroClickRule> _rules = [];
    private readonly HashSet<string> _disabledRules = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _recentExecutions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();
    private bool _disposed;

    public ZeroClickWorkflowService(
        IEventBus eventBus,
        IAppStateService appState,
        IFocusLockService focusLock,
        IWorkflowManager workflowManager,
        IZeroClickSafetyPolicy safetyPolicy,
        IFlowStateEngine flowStateEngine,
        IPerformanceMonitor perf,
        IRegionalSettingsService regional,
        ILogger<ZeroClickWorkflowService> logger)
    {
        _eventBus = eventBus;
        _appState = appState;
        _focusLock = focusLock;
        _workflowManager = workflowManager;
        _safetyPolicy = safetyPolicy;
        _flowStateEngine = flowStateEngine;
        _perf = perf;
        _regional = regional;
        _logger = logger;

        // Subscribe to domain events that may trigger zero-click rules
        _eventBus.Subscribe<UserLoggedInEvent>(OnTriggerEvent);
        _eventBus.Subscribe<OperationalModeChangedEvent>(OnTriggerEvent);
        _eventBus.Subscribe<ConnectionRestoredEvent>(OnTriggerEvent);

        // Re-evaluate when flow state changes — a transition to Flow may
        // promote Medium-confidence rules, a transition to Calm may block
        // DataEntry rules.
        _eventBus.Subscribe<FlowStateChangedEvent>(OnFlowStateChangedAsync);
    }

    // ── Public API ───────────────────────────────────────────────────

    public IReadOnlyList<string> RegisteredRuleIds
    {
        get { lock (_lock) return _rules.Select(r => r.RuleId).ToList(); }
    }

    public IReadOnlySet<string> DisabledRuleIds
    {
        get { lock (_lock) return new HashSet<string>(_disabledRules, StringComparer.OrdinalIgnoreCase); }
    }

    public void RegisterRule(IZeroClickRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        lock (_lock)
        {
            if (_rules.Any(r => r.RuleId.Equals(rule.RuleId, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Rule '{rule.RuleId}' is already registered.");

            _rules.Add(rule);
        }

        _logger.LogDebug("ZeroClick rule registered: {RuleId}", rule.RuleId);
    }

    public void DisableRule(string ruleId)
    {
        lock (_lock) _disabledRules.Add(ruleId);
        _logger.LogInformation("ZeroClick rule disabled: {RuleId}", ruleId);
    }

    public void EnableRule(string ruleId)
    {
        lock (_lock) _disabledRules.Remove(ruleId);
        _logger.LogInformation("ZeroClick rule enabled: {RuleId}", ruleId);
    }

    public bool IsRuleDisabled(string ruleId)
    {
        lock (_lock) return _disabledRules.Contains(ruleId);
    }

    public async Task EvaluateAllAsync(CancellationToken ct = default)
    {
        List<IZeroClickRule> snapshot;
        lock (_lock)
        {
            snapshot = [.. _rules];
        }

        foreach (var rule in snapshot)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                await EvaluateAndExecuteAsync(rule, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "ZeroClick rule '{RuleId}' threw during evaluation/execution.", rule.RuleId);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _eventBus.Unsubscribe<UserLoggedInEvent>(OnTriggerEvent);
        _eventBus.Unsubscribe<OperationalModeChangedEvent>(OnTriggerEvent);
        _eventBus.Unsubscribe<ConnectionRestoredEvent>(OnTriggerEvent);
        _eventBus.Unsubscribe<FlowStateChangedEvent>(OnFlowStateChangedAsync);
    }

    // ── Event handler ────────────────────────────────────────────────

    private Task OnTriggerEvent<TEvent>(TEvent @event) where TEvent : IEvent
    {
        // Clear de-duplication guard on each new trigger cycle
        lock (_lock) _recentExecutions.Clear();

        _logger.LogDebug(
            "ZeroClick evaluation triggered by {EventType}.", typeof(TEvent).Name);

        return EvaluateAllAsync();
    }

    private Task OnFlowStateChangedAsync(FlowStateChangedEvent evt)
    {
        // Clear de-duplication guard — flow state transitions are a
        // new context and rules should be re-evaluated from scratch.
        lock (_lock) _recentExecutions.Clear();

        _logger.LogDebug(
            "ZeroClick evaluation triggered by FlowState {Previous} → {Current}.",
            evt.Previous, evt.Current);

        return EvaluateAllAsync();
    }

    // ── Core logic ───────────────────────────────────────────────────

    private async Task EvaluateAndExecuteAsync(IZeroClickRule rule, CancellationToken ct)
    {
        // Gate 1: disabled?
        lock (_lock)
        {
            if (_disabledRules.Contains(rule.RuleId))
                return;
        }

        // Gate 2: already fired in this trigger cycle?
        lock (_lock)
        {
            if (_recentExecutions.Contains(rule.RuleId))
                return;
        }

        // Gate 3: global safety gates
        if (!PassesSafetyGates(rule.RuleId))
            return;

        // Gate 4: action category safety policy (hardcoded blocklist)
        var safetyVerdict = _safetyPolicy.Evaluate(rule.RuleId, rule.ActionCategory);
        if (!safetyVerdict.IsAllowed)
        {
            _logger.LogDebug(
                "ZeroClick rule '{RuleId}' blocked by safety policy: {Reason}",
                rule.RuleId, safetyVerdict.Reason);
            return;
        }

        // Gate 5: flow-state category restriction
        // In Calm mode, DataEntry is blocked — the operator is browsing,
        // not actively working in a billing session.
        var flowState = _flowStateEngine.CurrentState;
        if (FlowZeroClickAdapter.IsCategoryBlockedByFlowState(flowState, rule.ActionCategory))
        {
            _logger.LogDebug(
                "ZeroClick rule '{RuleId}' blocked by flow state {FlowState}: " +
                "category {Category} not allowed in this state.",
                rule.RuleId, flowState, rule.ActionCategory);
            return;
        }

        // Evaluate
        using var scope = _perf.BeginScope(
            $"ZeroClick.Evaluate.{rule.RuleId}", TimeSpan.FromMilliseconds(50));

        var evaluation = await rule.EvaluateAsync(ct).ConfigureAwait(false);

        // Flow-state confidence adaptation: in Flow mode, Medium is
        // promoted to High for faster auto-execution.
        var adaptedConfidence = FlowZeroClickAdapter.AdaptConfidence(flowState, evaluation.Confidence);

        if (adaptedConfidence != ZeroClickConfidence.High)
        {
            if (evaluation.Confidence == ZeroClickConfidence.Medium)
            {
                _logger.LogDebug(
                    "ZeroClick rule '{RuleId}' blocked: {Description} (flow={FlowState})",
                    rule.RuleId, evaluation.Description, flowState);
            }
            return;
        }

        // De-duplicate
        lock (_lock)
        {
            if (!_recentExecutions.Add(rule.RuleId))
                return;
        }

        // Execute
        _logger.LogInformation(
            "ZeroClick rule '{RuleId}' auto-executing: {Description} (flow={FlowState}, promoted={Promoted})",
            rule.RuleId, evaluation.Description, flowState,
            adaptedConfidence != evaluation.Confidence);

        await rule.ExecuteAsync(ct).ConfigureAwait(false);

        await _eventBus.PublishAsync(new ZeroClickActionExecutedEvent(
            rule.RuleId, evaluation.Description, _regional.Now)).ConfigureAwait(false);
    }

    private bool PassesSafetyGates(string ruleId)
    {
        // Gate: not offline
        if (_appState.IsOfflineMode)
        {
            _logger.LogDebug("ZeroClick '{RuleId}' skipped: app is offline.", ruleId);
            return false;
        }

        // Gate: no active workflow running
        if (_workflowManager.IsRunning)
        {
            _logger.LogDebug("ZeroClick '{RuleId}' skipped: workflow is active.", ruleId);
            return false;
        }

        // Gate: focus not locked (billing mode)
        if (_focusLock.IsFocusLocked)
        {
            _logger.LogDebug("ZeroClick '{RuleId}' skipped: focus is locked.", ruleId);
            return false;
        }

        return true;
    }
}
