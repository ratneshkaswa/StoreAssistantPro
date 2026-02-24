using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Models;

/// <summary>
/// Immutable snapshot of an operator's experience profile, used by the
/// adaptive tip system to decide which guidance is visible and whether
/// onboarding features are active.
///
/// <para><b>Core responsibilities:</b></para>
/// <list type="bullet">
///   <item>Maps <see cref="UserExperienceLevel"/> to the maximum
///         <see cref="TipLevel"/> the operator should see
///         (<see cref="MaxVisibleTipLevel"/>).</item>
///   <item>Exposes <see cref="IsOnboarding"/> to let the UI show
///         extra inline guidance during the beginner phase.</item>
///   <item>Carries the <see cref="CompletedSessionCount"/> so
///         promotion logic can decide when to auto-advance the
///         operator to the next level.</item>
/// </list>
///
/// <para><b>Lifetime &amp; ownership:</b></para>
/// <para>
/// Profiles are created by service code (e.g.
/// <see cref="ITipRotationService"/> or a future
/// <c>IUserExperienceService</c>) and consumed read-only by the
/// tip pipeline, the help-context system, and any UI that
/// adapts to operator experience.
/// </para>
///
/// <para><b>Immutability:</b> All properties are <c>init</c>-only.
/// To change the level, create a new profile via the <c>with</c>
/// expression:</para>
/// <code>
/// var promoted = currentProfile with
/// {
///     Level = UserExperienceLevel.Intermediate
/// };
/// </code>
///
/// <para><b>Usage — tip filtering:</b></para>
/// <code>
/// // In TipRotationService or TipRegistryService:
/// var profile = UserExperienceProfile.For(
///     UserExperienceLevel.Intermediate, sessionCount: 8);
///
/// var tip = registry.Resolve(
///     "SalesView",
///     profile.MaxVisibleTipLevel,
///     helpContext);
/// </code>
///
/// <para><b>Auto-promotion thresholds (defaults):</b></para>
/// <list type="table">
///   <listheader>
///     <term>From → To</term>
///     <description>Session threshold</description>
///   </listheader>
///   <item>
///     <term>Beginner → Intermediate</term>
///     <description><see cref="DefaultBeginnerThreshold"/> (3 sessions)</description>
///   </item>
///   <item>
///     <term>Intermediate → Advanced</term>
///     <description><see cref="DefaultIntermediateThreshold"/> (10 sessions)</description>
///   </item>
/// </list>
/// </summary>
public sealed record UserExperienceProfile
{
    // ── Auto-promotion thresholds ──────────────────────────────────

    /// <summary>
    /// Default number of completed sessions before an operator is
    /// auto-promoted from <see cref="UserExperienceLevel.Beginner"/>
    /// to <see cref="UserExperienceLevel.Intermediate"/>.
    /// </summary>
    public const int DefaultBeginnerThreshold = 3;

    /// <summary>
    /// Default number of completed sessions before an operator is
    /// auto-promoted from <see cref="UserExperienceLevel.Intermediate"/>
    /// to <see cref="UserExperienceLevel.Advanced"/>.
    /// </summary>
    public const int DefaultIntermediateThreshold = 10;

    // ── Properties ─────────────────────────────────────────────────

    /// <summary>
    /// The operator's current experience classification.
    /// Drives tip filtering, onboarding state, and any UI that
    /// adapts to operator expertise.
    /// </summary>
    public UserExperienceLevel Level { get; init; }

    /// <summary>
    /// Total number of login sessions the operator has completed.
    /// Used by auto-promotion logic to decide when to advance the
    /// operator to the next <see cref="UserExperienceLevel"/>.
    /// </summary>
    public int CompletedSessionCount { get; init; }

    /// <summary>
    /// The maximum <see cref="TipLevel"/> this operator should see.
    /// Computed from <see cref="Level"/> — always consistent, no
    /// separate storage.
    /// <list type="table">
    ///   <listheader>
    ///     <term>Experience Level</term>
    ///     <description>Max Tip Level</description>
    ///   </listheader>
    ///   <item>
    ///     <term><see cref="UserExperienceLevel.Beginner"/></term>
    ///     <description><see cref="TipLevel.Beginner"/></description>
    ///   </item>
    ///   <item>
    ///     <term><see cref="UserExperienceLevel.Intermediate"/></term>
    ///     <description><see cref="TipLevel.Normal"/></description>
    ///   </item>
    ///   <item>
    ///     <term><see cref="UserExperienceLevel.Advanced"/></term>
    ///     <description><see cref="TipLevel.Advanced"/></description>
    ///   </item>
    /// </list>
    /// <para>
    /// Pass this value to
    /// <see cref="ITipRegistryService.Resolve(string, TipLevel, HelpContext)"/>
    /// as the <c>maxLevel</c> parameter.
    /// </para>
    /// </summary>
    public TipLevel MaxVisibleTipLevel => Level switch
    {
        UserExperienceLevel.Beginner     => TipLevel.Beginner,
        UserExperienceLevel.Intermediate => TipLevel.Normal,
        UserExperienceLevel.Advanced     => TipLevel.Advanced,
        _ => TipLevel.Normal
    };

    /// <summary>
    /// <c>true</c> when the operator is in the beginner onboarding
    /// phase and should see extra inline guidance (walkthroughs,
    /// animated hints, expanded help panels).
    /// </summary>
    public bool IsOnboarding => Level == UserExperienceLevel.Beginner;

    /// <summary>
    /// Minimum interval between tip displays for the same window.
    /// Drives progressive tip reduction — beginners see tips on every
    /// view load, intermediate operators are shown tips less often,
    /// and advanced operators rarely see tips.
    /// <list type="table">
    ///   <listheader>
    ///     <term>Experience Level</term>
    ///     <description>Cooldown</description>
    ///   </listheader>
    ///   <item>
    ///     <term><see cref="UserExperienceLevel.Beginner"/></term>
    ///     <description><see cref="TimeSpan.Zero"/> — every visit</description>
    ///   </item>
    ///   <item>
    ///     <term><see cref="UserExperienceLevel.Intermediate"/></term>
    ///     <description>5 minutes — moderate reduction</description>
    ///   </item>
    ///   <item>
    ///     <term><see cref="UserExperienceLevel.Advanced"/></term>
    ///     <description>30 minutes — minimal tips</description>
    ///   </item>
    /// </list>
    /// <para>
    /// Consumed by <see cref="ITipRotationService"/> to suppress
    /// repeat tip displays within the cooldown window. One-time
    /// (<see cref="TipDefinition.IsOneTime"/>) tips that have not
    /// yet been shown bypass the cooldown — they are too important
    /// to delay.
    /// </para>
    /// </summary>
    public TimeSpan TipCooldown => Level switch
    {
        UserExperienceLevel.Beginner     => TimeSpan.Zero,
        UserExperienceLevel.Intermediate => TimeSpan.FromMinutes(5),
        UserExperienceLevel.Advanced     => TimeSpan.FromMinutes(30),
        _ => TimeSpan.FromMinutes(5)
    };

    /// <summary>
    /// <c>true</c> when the operator's session count has reached the
    /// threshold for the next experience level — a promotion is
    /// available but has not yet been applied.
    /// </summary>
    public bool IsPromotionAvailable => Level switch
    {
        UserExperienceLevel.Beginner
            => CompletedSessionCount >= DefaultBeginnerThreshold,
        UserExperienceLevel.Intermediate
            => CompletedSessionCount >= DefaultIntermediateThreshold,
        _ => false
    };

    /// <summary>
    /// Returns the next <see cref="UserExperienceLevel"/> the operator
    /// would be promoted to, or <c>null</c> if already at
    /// <see cref="UserExperienceLevel.Advanced"/>.
    /// </summary>
    public UserExperienceLevel? NextLevel => Level switch
    {
        UserExperienceLevel.Beginner      => UserExperienceLevel.Intermediate,
        UserExperienceLevel.Intermediate  => UserExperienceLevel.Advanced,
        _                                 => null
    };

    // ── Factory methods ────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="UserExperienceProfile"/> for the given
    /// <paramref name="level"/> and <paramref name="sessionCount"/>.
    /// <see cref="MaxVisibleTipLevel"/> is computed automatically.
    /// </summary>
    /// <param name="level">The operator's current experience level.</param>
    /// <param name="sessionCount">
    /// Number of completed login sessions. Defaults to <c>0</c>.
    /// </param>
    public static UserExperienceProfile For(
        UserExperienceLevel level,
        int sessionCount = 0) => new()
    {
        Level = level,
        CompletedSessionCount = sessionCount
    };

    /// <summary>
    /// Creates a beginner profile — convenience shorthand for
    /// first-time setup and default state.
    /// </summary>
    public static UserExperienceProfile Default { get; } =
        For(UserExperienceLevel.Beginner);

    /// <summary>
    /// Returns a new profile promoted to the next level if
    /// <see cref="IsPromotionAvailable"/> is <c>true</c>;
    /// otherwise returns <c>this</c> unchanged.
    /// </summary>
    public UserExperienceProfile TryPromote()
    {
        if (!IsPromotionAvailable || NextLevel is not { } next)
            return this;

        return this with { Level = next };
    }
}
