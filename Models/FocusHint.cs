namespace StoreAssistantPro.Models;

/// <summary>
/// Describes a recommended keyboard focus target produced by
/// <see cref="Core.Services.IPredictiveFocusService"/> after a
/// workflow transition.
/// <para>
/// Consumers (attached behaviors, ViewModels) inspect the hint's
/// <see cref="Strategy"/> to determine how to resolve the target
/// element. The service never touches the visual tree directly.
/// </para>
/// </summary>
public sealed record FocusHint
{
    /// <summary>
    /// How the focus target should be resolved in the visual tree.
    /// </summary>
    public required FocusStrategy Strategy { get; init; }

    /// <summary>
    /// When <see cref="Strategy"/> is <see cref="FocusStrategy.Named"/>,
    /// the <c>x:Name</c> or <c>AutomationProperties.AutomationId</c>
    /// of the target element. Otherwise <see cref="string.Empty"/>.
    /// </summary>
    public string ElementName { get; init; } = string.Empty;

    /// <summary>
    /// The workflow context that produced this hint (e.g.,
    /// <c>"NavigatedToProducts"</c>, <c>"BillingLockAcquired"</c>).
    /// Used for diagnostics and logging — never drives logic.
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Priority when multiple hints are queued in the same dispatcher
    /// frame. Higher wins. Default 0.
    /// </summary>
    public int Priority { get; init; }

    /// <summary>Hint that requests focus on the first focusable input.</summary>
    public static FocusHint FirstInput(string reason, int priority = 0) => new()
    {
        Strategy = FocusStrategy.FirstInput,
        Reason = reason,
        Priority = priority
    };

    /// <summary>Hint that requests focus on a named element.</summary>
    public static FocusHint Named(string elementName, string reason, int priority = 0) => new()
    {
        Strategy = FocusStrategy.Named,
        ElementName = elementName,
        Reason = reason,
        Priority = priority
    };

    /// <summary>Hint that requests focus to remain unchanged.</summary>
    public static FocusHint Preserve(string reason) => new()
    {
        Strategy = FocusStrategy.Preserve,
        Reason = reason
    };
}
