using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Provides context-aware help text based on the current application
/// state (operational mode, connectivity, focus lock, active module)
/// and the operator's <see cref="UserExperienceLevel"/>.
/// <para>
/// <b>Responsibilities:</b>
/// </para>
/// <list type="bullet">
///   <item>Builds and caches the current <see cref="HelpContext"/> from
///         <see cref="IAppStateService"/>, <see cref="IFocusLockService"/>,
///         and <see cref="IOnboardingJourneyService"/>.</item>
///   <item>Reacts to <see cref="Events.OperationalModeChangedEvent"/>,
///         <see cref="Events.OfflineModeChangedEvent"/>,
///         <see cref="Events.ExperienceLevelPromotedEvent"/>, and
///         focus-lock <c>PropertyChanged</c> to refresh automatically.</item>
///   <item>Publishes <see cref="Events.HelpContextChangedEvent"/> so
///         consumers (tooltips, panels) can update without polling.</item>
///   <item>Resolves context-specific help text and usage tips via an
///         ordered rule pipeline. Rules are evaluated top-down; the
///         first match wins.</item>
///   <item>Post-processes the result through an experience-level adapter:
///         Beginner users receive detailed, step-by-step explanations;
///         Advanced users receive short, concise action-oriented text;
///         Intermediate users see the default rule output unchanged.</item>
/// </list>
/// <para>
/// Registered as a <b>singleton</b>. Implements <see cref="IDisposable"/>
/// to unsubscribe from the event bus at shutdown.
/// </para>
/// </summary>
public interface IContextHelpService : IDisposable
{
    /// <summary>
    /// Current application context snapshot. Updated automatically
    /// when mode, connectivity, or focus-lock state changes.
    /// </summary>
    HelpContext CurrentContext { get; }

    /// <summary>
    /// Resolves a context-aware help result for the specified key.
    /// Runs the rule pipeline against the current <see cref="CurrentContext"/>
    /// and returns the winning <see cref="ContextHelpResult"/>, or
    /// <c>null</c> when no rule matches.
    /// </summary>
    ContextHelpResult? Resolve(string key);
}

/// <summary>
/// Immutable result of context-aware help resolution.
/// <see cref="Description"/> and <see cref="UsageTip"/> map directly
/// to <c>SmartTooltip.Text</c> and <c>SmartTooltip.UsageTip</c>.
/// <see cref="Suffix"/> is appended to the description (e.g. offline
/// warnings) when present.
/// <para>
/// <see cref="EffectiveDescription"/> is eagerly computed by the
/// constructor and cached — zero per-access string allocation.
/// The <c>with</c> expression copies all properties and re-runs the
/// constructor, so the cache is always consistent.
/// </para>
/// </summary>
public sealed record ContextHelpResult(
    string? Description,
    string? UsageTip,
    string? Suffix)
{
    /// <summary>
    /// Returns <see cref="Description"/> with <see cref="Suffix"/>
    /// appended (separated by a space) when both are present.
    /// When only <see cref="Suffix"/> is set the suffix is returned
    /// alone so it can serve as the tooltip body for elements that
    /// have no static description.
    /// <para>Eagerly computed once — zero per-access cost.</para>
    /// </summary>
    public string? EffectiveDescription { get; } =
        (Description, Suffix) switch
        {
            (not null, not null) => $"{Description} {Suffix}",
            (not null, null)     => Description,
            (null, not null)     => Suffix,
            _                    => null,
        };
}
