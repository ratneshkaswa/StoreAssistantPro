using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Intelligent tip rotation engine that selects the next tip to show
/// in an <c>InlineTipBanner</c> slot, avoiding repetition, respecting
/// priority and dismiss state, and adapting tip visibility to the
/// operator's <see cref="UserExperienceLevel"/>.
///
/// <para><b>Rotation algorithm:</b></para>
/// <list type="number">
///   <item><b>Candidate filtering</b> — delegates to
///         <see cref="ITipRegistryService"/> for window, level,
///         context, and dismiss filtering.</item>
///   <item><b>Experience-level gating</b> — reads
///         <see cref="IOnboardingJourneyService.CurrentProfile"/> to
///         obtain <see cref="UserExperienceProfile.MaxVisibleTipLevel"/>.
///         Beginner operators see only <see cref="TipLevel.Beginner"/>
///         tips; Intermediate see up to <see cref="TipLevel.Normal"/>;
///         Advanced see all levels.</item>
///   <item><b>Progressive frequency reduction</b> — a per-window
///         cooldown driven by
///         <see cref="UserExperienceProfile.TipCooldown"/> throttles
///         tip display frequency. Beginner = every visit (no cooldown),
///         Intermediate = 5 min, Advanced = 30 min. One-time tips
///         that have never been shown bypass the cooldown.</item>
///   <item><b>Recency suppression</b> — tips that were shown in
///         the last <em>N</em> rotations are pushed to the back of
///         the queue so operators see variety, not the same tip
///         every time they visit a view.</item>
///   <item><b>Priority weighting</b> — among non-recent candidates,
///         higher-priority tips are strongly preferred. A tip at
///         priority 80 is chosen before one at 40, but both will
///         eventually rotate through.</item>
/// </list>
///
/// <para><b>Lifetime:</b> Registered as a <b>singleton</b>.
/// Recency history is kept in-memory (per-application-session) and
/// resets on restart — this is intentional so tips reappear across
/// sessions for reinforcement.</para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// // Called by TipBannerAutoState or a ViewModel when a view loads:
/// var tip = rotation.GetNextTip("SalesView", context);
/// if (tip is not null)
/// {
///     banner.Title   = tip.Title;
///     banner.TipText = tip.Message;
/// }
///
/// // Called when context changes (mode switch, offline transition):
/// rotation.Invalidate("SalesView");
/// var fresh = rotation.GetNextTip("SalesView", context);
/// </code>
/// </summary>
public interface ITipRotationService
{
    /// <summary>
    /// Selects the next tip to display for <paramref name="windowName"/>
    /// given the current <paramref name="context"/>. The selection
    /// accounts for the operator's <see cref="UserExperienceLevel"/>,
    /// recency, priority, and dismiss state.
    /// </summary>
    /// <param name="windowName">
    /// Unqualified view/window type name, e.g. <c>"SalesView"</c>.
    /// </param>
    /// <param name="context">
    /// Current application context snapshot (mode, connectivity,
    /// focus lock, user role).
    /// </param>
    /// <returns>
    /// The winning <see cref="TipDefinition"/>, or <c>null</c> when
    /// no eligible, non-dismissed, non-recently-shown tip exists.
    /// </returns>
    TipDefinition? GetNextTip(string windowName, HelpContext context);

    /// <summary>
    /// Clears the cached rotation result for
    /// <paramref name="windowName"/> so the next
    /// <see cref="GetNextTip"/> call re-evaluates from scratch.
    /// <para>
    /// Call this when the application context changes (mode switch,
    /// offline transition, focus-lock change) or when a tip is
    /// dismissed so the banner can show a fresh tip immediately.
    /// </para>
    /// </summary>
    void Invalidate(string windowName);

    /// <summary>
    /// Clears all cached rotation results across every window.
    /// Typically called on login/logout or session reset.
    /// </summary>
    void InvalidateAll();

    /// <summary>
    /// Records that a tip was dismissed by the user so the rotation
    /// engine can immediately select a replacement. Delegates the
    /// persist to <see cref="ITipStateService"/> and calls
    /// <see cref="Invalidate"/> for the tip's window.
    /// </summary>
    /// <param name="tip">The dismissed tip.</param>
    void RecordDismissal(TipDefinition tip);

    /// <summary>
    /// Notifies the rotation engine that a new user session has
    /// started. The engine increments its internal session counter
    /// which drives onboarding-phase filtering.
    /// </summary>
    void NotifySessionStart();

    /// <summary>
    /// Returns the number of sessions the rotation engine has
    /// observed since the application was installed (or since the
    /// counter was last reset). Used by the onboarding filter.
    /// </summary>
    int SessionCount { get; }

    /// <summary>
    /// Legacy session-count threshold retained for backward
    /// compatibility. The primary tip-level ceiling is now driven by
    /// <see cref="IOnboardingJourneyService.CurrentProfile"/> via
    /// <see cref="UserExperienceProfile.MaxVisibleTipLevel"/>.
    /// <para>
    /// This property is still incremented by
    /// <see cref="NotifySessionStart"/> and can be queried for
    /// diagnostics, but it no longer gates tip visibility directly.
    /// </para>
    /// </summary>
    int OnboardingSessionThreshold { get; set; }
}
