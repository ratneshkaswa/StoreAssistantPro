namespace StoreAssistantPro.Core.Workflows;

/// <summary>
/// A single zero-click automation rule. Implementations detect a specific
/// high-confidence scenario and execute the corresponding action when safe.
/// <para>
/// <b>Contract:</b>
/// <list type="bullet">
///   <item><see cref="EvaluateAsync"/> must be <b>pure</b> — no side effects,
///         no state mutation. It only inspects current application state.</item>
///   <item><see cref="ExecuteAsync"/> performs the action. It may call
///         <see cref="Commands.ICommandBus"/> or services.</item>
///   <item>Rules must be <b>idempotent</b> — executing the same rule twice
///         for the same trigger must not cause duplicate work.</item>
/// </list>
/// </para>
/// </summary>
public interface IZeroClickRule
{
    /// <summary>
    /// Unique identifier for this rule (e.g., <c>"AutoNavigateAfterLogin"</c>).
    /// Used for logging, telemetry, and the disable list.
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// The category of action this rule performs. The
    /// <see cref="IZeroClickSafetyPolicy"/> uses this to enforce
    /// hardcoded safety restrictions — rules categorised as
    /// <see cref="ZeroClickActionCategory.Delete"/>,
    /// <see cref="ZeroClickActionCategory.SettingsChange"/>,
    /// <see cref="ZeroClickActionCategory.FinancialConfirmation"/>, or
    /// <see cref="ZeroClickActionCategory.SecuritySensitive"/> are
    /// permanently forbidden from automatic execution.
    /// <para>
    /// Defaults to <see cref="ZeroClickActionCategory.ReadOnly"/> for
    /// backward compatibility with existing rules that do not override
    /// this property.
    /// </para>
    /// </summary>
    ZeroClickActionCategory ActionCategory => ZeroClickActionCategory.ReadOnly;

    /// <summary>
    /// Evaluates whether the rule's conditions are met in the current
    /// application state. Returns confidence level and description.
    /// <b>Must be side-effect-free.</b>
    /// </summary>
    Task<ZeroClickEvaluation> EvaluateAsync(CancellationToken ct = default);

    /// <summary>
    /// Executes the automatic action. Called only when
    /// <see cref="EvaluateAsync"/> returned <see cref="ZeroClickConfidence.High"/>.
    /// </summary>
    Task ExecuteAsync(CancellationToken ct = default);
}
