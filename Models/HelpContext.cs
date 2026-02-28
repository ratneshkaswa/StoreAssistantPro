using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Models;

/// <summary>
/// Immutable snapshot of the current application context, used to
/// drive context-aware help content (tooltips, help overlays,
/// guided walkthroughs).
///
/// <para><b>Fields:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Property</term>
///     <description>Source</description>
///   </listheader>
///   <item>
///     <term><see cref="OperationalMode"/></term>
///     <description><see cref="IAppStateService.CurrentMode"/></description>
///   </item>
///   <item>
///     <term><see cref="IsOfflineMode"/></term>
///     <description><see cref="IAppStateService.IsOfflineMode"/></description>
///   </item>
///   <item>
///     <term><see cref="IsFocusLocked"/></term>
///     <description><see cref="IFocusLockService.IsFocusLocked"/></description>
///   </item>
///   <item>
///     <term><see cref="CurrentModule"/></term>
///     <description><see cref="IFocusLockService.ActiveModule"/> or the
///     shell's active navigation key.</description>
///   </item>
///   <item>
///     <term><see cref="UserRole"/></term>
///     <description><see cref="IAppStateService.CurrentUserType"/> —
///     reserved for future role-sensitive help filtering.</description>
///   </item>
///   <item>
///     <term><see cref="ExperienceLevel"/></term>
///     <description><see cref="IOnboardingJourneyService.CurrentProfile"/> via
///     <see cref="UserExperienceProfile.Level"/> — drives adaptive help
///     verbosity.</description>
///   </item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// var ctx = HelpContext.From(appState, focusLock);
/// // Filter help entries by context
/// var hints = helpProvider.GetHints(ctx);
/// </code>
/// </summary>
public sealed record HelpContext
{
    /// <summary>
    /// Active operational mode (<see cref="Models.OperationalMode.Management"/>
    /// or <see cref="Models.OperationalMode.Billing"/>).
    /// </summary>
    public required OperationalMode OperationalMode { get; init; }

    /// <summary>
    /// <c>true</c> when the database is unreachable and the app is
    /// running in degraded/offline mode.
    /// </summary>
    public required bool IsOfflineMode { get; init; }

    /// <summary>
    /// <c>true</c> when the UI focus is locked to a specific module
    /// (e.g. during an active billing session).
    /// </summary>
    public required bool IsFocusLocked { get; init; }

    /// <summary>
    /// Identifier of the currently active module (e.g. "Billing",
    /// "Products", "Sales"). Empty when no module holds focus.
    /// </summary>
    public required string CurrentModule { get; init; }

    /// <summary>
    /// Role of the authenticated user. Reserved for future
    /// role-sensitive help filtering (e.g. admin-only tips).
    /// </summary>
    public UserType? UserRole { get; init; }

    /// <summary>
    /// The operator's current experience level. Drives adaptive help
    /// verbosity — beginner users receive detailed explanations while
    /// advanced users receive short, concise tips.
    /// <para>
    /// Defaults to <see cref="UserExperienceLevel.Intermediate"/> when
    /// the onboarding service is unavailable.
    /// </para>
    /// </summary>
    public UserExperienceLevel ExperienceLevel { get; init; } = UserExperienceLevel.Intermediate;

    /// <summary>
    /// Creates a <see cref="HelpContext"/> snapshot from the current
    /// application state, focus-lock, and onboarding services.
    /// </summary>
    public static HelpContext From(
        IAppStateService appState,
        IFocusLockService focusLock,
        IOnboardingJourneyService? onboarding = null) => new()
    {
        OperationalMode = appState.CurrentMode,
        IsOfflineMode = appState.IsOfflineMode,
        IsFocusLocked = focusLock.IsFocusLocked,
        CurrentModule = focusLock.ActiveModule,
        UserRole = appState.IsLoggedIn ? appState.CurrentUserType : null,
        ExperienceLevel = onboarding?.CurrentProfile.Level ?? UserExperienceLevel.Intermediate,
    };
}
