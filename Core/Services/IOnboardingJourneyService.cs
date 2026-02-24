using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Tracks operator usage milestones and automatically promotes the
/// <see cref="UserExperienceLevel"/> when predefined thresholds are
/// reached.
///
/// <para><b>Responsibilities:</b></para>
/// <list type="bullet">
///   <item>Records discrete user actions (window opens, completed
///         billing sessions, navigation events) in an in-memory
///         counter set that is persisted to a local JSON file.</item>
///   <item>Evaluates promotion rules after every action — when a
///         rule fires, the operator's profile is advanced to the
///         next <see cref="UserExperienceLevel"/>.</item>
///   <item>Publishes <see cref="Events.ExperienceLevelPromotedEvent"/>
///         so downstream systems (tip rotation, banner state, settings
///         UI) can react immediately.</item>
///   <item>Exposes the current <see cref="UserExperienceProfile"/>
///         for consumption by the tip pipeline and any UI that adapts
///         to operator experience.</item>
/// </list>
///
/// <para><b>Action types (tracked):</b></para>
/// <list type="table">
///   <listheader>
///     <term>Action</term>
///     <description>Recorded by</description>
///   </listheader>
///   <item>
///     <term><c>WindowOpened</c></term>
///     <description>Called by view code-behind or ViewModel on load.</description>
///   </item>
///   <item>
///     <term><c>BillingCompleted</c></term>
///     <description>Observed via <see cref="IAppStateService.CurrentBillingSession"/>
///     transitioning to <see cref="BillingSessionState.Completed"/>.</description>
///   </item>
///   <item>
///     <term><c>SessionStarted</c></term>
///     <description>Called by login flow; also forwarded to
///     <see cref="ITipRotationService.NotifySessionStart"/>.</description>
///   </item>
/// </list>
///
/// <para><b>Promotion rules (defaults):</b></para>
/// <list type="table">
///   <listheader>
///     <term>Transition</term>
///     <description>Condition</description>
///   </listheader>
///   <item>
///     <term>Beginner → Intermediate</term>
///     <description>≥ 5 distinct windows opened <b>OR</b>
///     ≥ 3 completed sessions.</description>
///   </item>
///   <item>
///     <term>Intermediate → Advanced</term>
///     <description>≥ 5 completed billing sessions <b>OR</b>
///     ≥ 10 completed sessions.</description>
///   </item>
/// </list>
///
/// <para><b>Lifetime:</b> Registered as a <b>singleton</b>.
/// Implements <see cref="IDisposable"/> to unsubscribe from the
/// event bus at shutdown.</para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// // Record a window open (e.g. in a view code-behind):
/// onboarding.RecordWindowOpen("SalesView");
///
/// // Query the current profile (e.g. in TipRotationService):
/// var profile = onboarding.CurrentProfile;
/// var maxTip  = profile.MaxVisibleTipLevel;
///
/// // Override level manually (admin settings):
/// onboarding.SetLevel(UserExperienceLevel.Advanced);
/// </code>
/// </summary>
public interface IOnboardingJourneyService : IDisposable
{
    /// <summary>
    /// Current experience profile snapshot. Updated after every
    /// recorded action that triggers a promotion check.
    /// </summary>
    UserExperienceProfile CurrentProfile { get; }

    /// <summary>
    /// Records that the operator opened a window or navigated to
    /// a view. The <paramref name="windowName"/> is counted as a
    /// distinct milestone — opening the same window multiple times
    /// increments the total-opens counter but the distinct-windows
    /// set only grows on first visit.
    /// </summary>
    /// <param name="windowName">
    /// Unqualified view/window type name, e.g. <c>"SalesView"</c>,
    /// <c>"ProductsView"</c>.
    /// </param>
    void RecordWindowOpen(string windowName);

    /// <summary>
    /// Records that a billing session completed successfully.
    /// Called automatically when <see cref="IAppStateService.CurrentBillingSession"/>
    /// transitions to <see cref="BillingSessionState.Completed"/>.
    /// </summary>
    void RecordBillingCompleted();

    /// <summary>
    /// Records that a new login session has started. Increments the
    /// session counter and forwards to
    /// <see cref="ITipRotationService.NotifySessionStart"/>.
    /// </summary>
    void RecordSessionStart();

    /// <summary>
    /// Manually sets the operator's experience level, bypassing
    /// automatic promotion rules. Used by the admin settings UI.
    /// </summary>
    /// <param name="level">The desired experience level.</param>
    void SetLevel(UserExperienceLevel level);

    /// <summary>
    /// Resets all tracked milestones and reverts the operator to
    /// <see cref="UserExperienceLevel.Beginner"/>. Used by
    /// "Reset onboarding" in system settings.
    /// </summary>
    void Reset();

    // ── Observable counters (diagnostics / settings UI) ─────────

    /// <summary>Total number of login sessions recorded.</summary>
    int TotalSessions { get; }

    /// <summary>Number of distinct window names opened.</summary>
    int DistinctWindowsOpened { get; }

    /// <summary>Total number of window-open events recorded.</summary>
    int TotalWindowOpens { get; }

    /// <summary>Total number of completed billing sessions recorded.</summary>
    int TotalBillingCompleted { get; }
}
