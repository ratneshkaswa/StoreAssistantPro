namespace StoreAssistantPro.Core.Workflows;

/// <summary>
/// Singleton service that detects high-confidence user actions and
/// executes them automatically when all safety gates pass.
/// <para>
/// <b>Architecture:</b>
/// <list type="bullet">
///   <item>Event-driven — subscribes to domain events via
///         <see cref="Events.IEventBus"/> and re-evaluates registered
///         <see cref="IZeroClickRule"/> instances on each trigger.</item>
///   <item>Safe execution only — a rule fires only when
///         <see cref="ZeroClickConfidence.High"/> is returned by
///         <see cref="IZeroClickRule.EvaluateAsync"/>.</item>
///   <item>Command pipeline integration — rules that invoke commands
///         go through <see cref="Commands.ICommandBus"/> so validation,
///         logging, offline, and transaction behaviors still apply.</item>
///   <item>Disabled rules — individual rules can be disabled at
///         runtime without restarting (e.g., from System Settings).</item>
/// </list>
/// </para>
/// <para>
/// <b>Safety gates (checked before every execution):</b>
/// <list type="number">
///   <item>Application is not offline.</item>
///   <item>No billing session is active (or the rule explicitly
///         opts in to billing-mode execution).</item>
///   <item>No workflow is currently running.</item>
///   <item>The rule is not in the disabled list.</item>
///   <item>The rule has not already fired for this trigger
///         (de-duplication guard).</item>
/// </list>
/// </para>
/// </summary>
public interface IZeroClickWorkflowService : IDisposable
{
    /// <summary>
    /// Registers a rule for automatic evaluation. Typically called
    /// during DI composition (module registration).
    /// </summary>
    void RegisterRule(IZeroClickRule rule);

    /// <summary>
    /// Triggers evaluation of all registered rules. Called internally
    /// by event handlers but can be invoked explicitly for testing or
    /// manual re-evaluation.
    /// </summary>
    Task EvaluateAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Disables a rule by ID. The rule remains registered but will
    /// not be evaluated or executed until re-enabled.
    /// </summary>
    void DisableRule(string ruleId);

    /// <summary>
    /// Re-enables a previously disabled rule.
    /// </summary>
    void EnableRule(string ruleId);

    /// <summary>
    /// Returns <c>true</c> if the rule is currently disabled.
    /// </summary>
    bool IsRuleDisabled(string ruleId);

    /// <summary>
    /// Returns the IDs of all registered rules.
    /// </summary>
    IReadOnlyList<string> RegisteredRuleIds { get; }

    /// <summary>
    /// Returns the IDs of all disabled rules.
    /// </summary>
    IReadOnlySet<string> DisabledRuleIds { get; }
}
