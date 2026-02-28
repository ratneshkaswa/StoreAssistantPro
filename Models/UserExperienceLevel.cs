namespace StoreAssistantPro.Models;

/// <summary>
/// Classifies an operator's expertise with the application, used to
/// control onboarding intensity and tip visibility across the
/// adaptive guidance system.
///
/// <para><b>Relationship to <see cref="TipLevel"/>:</b></para>
/// <para>
/// <see cref="TipLevel"/> classifies <em>tips</em>;
/// <see cref="UserExperienceLevel"/> classifies <em>operators</em>.
/// The tip system uses the comparison
/// <c>tip.Level &lt;= profile.MaxVisibleTipLevel</c> to decide
/// which tips an operator sees — see
/// <see cref="UserExperienceProfile.MaxVisibleTipLevel"/>.
/// </para>
///
/// <para><b>Ordering:</b> The underlying <see cref="int"/> values
/// increase with expertise so level comparisons are simple
/// integer operations.</para>
///
/// <para><b>Transition guidance:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Level</term>
///     <description>When to assign</description>
///   </listheader>
///   <item>
///     <term><see cref="Beginner"/></term>
///     <description>First-day operators. Only essential core-workflow
///     tips are shown; onboarding walkthroughs are enabled; advanced
///     shortcuts are hidden to reduce cognitive load.</description>
///   </item>
///   <item>
///     <term><see cref="Intermediate"/></term>
///     <description>Operators who have completed a few sessions and
///     are comfortable with the basics. Productivity tips (keyboard
///     shortcuts, filter techniques) become visible; onboarding
///     walkthroughs are disabled.</description>
///   </item>
///   <item>
///     <term><see cref="Advanced"/></term>
///     <description>Experienced staff who have used the system
///     extensively. All tip levels are visible; power-user features
///     (bulk operations, keyboard-only flows) are surfaced.</description>
///   </item>
/// </list>
///
/// <para><b>Promotion:</b> Levels can be promoted automatically by
/// the tip rotation engine (based on session count) or manually by
/// an administrator via System Settings → General.</para>
/// </summary>
public enum UserExperienceLevel
{
    /// <summary>
    /// New operator — first day or first few sessions.
    /// <para>
    /// Only <see cref="TipLevel.Beginner"/> tips are shown.
    /// Onboarding walkthroughs are active. The UI may display
    /// additional inline guidance (e.g. animated arrows, expanded
    /// help panels) to accelerate learning.
    /// </para>
    /// </summary>
    Beginner = 0,

    /// <summary>
    /// Comfortable operator — knows the core workflows and is ready
    /// for productivity enhancements.
    /// <para>
    /// <see cref="TipLevel.Beginner"/> and <see cref="TipLevel.Normal"/>
    /// tips are shown. Keyboard shortcut hints and filter tips
    /// appear. Onboarding walkthroughs are disabled.
    /// </para>
    /// </summary>
    Intermediate = 1,

    /// <summary>
    /// Experienced operator — all features are unlocked.
    /// <para>
    /// All tip levels (<see cref="TipLevel.Beginner"/>,
    /// <see cref="TipLevel.Normal"/>, <see cref="TipLevel.Advanced"/>)
    /// are visible. Power-user tips for bulk operations, advanced
    /// discounts, and keyboard-only workflows are surfaced.
    /// </para>
    /// </summary>
    Advanced = 2
}
