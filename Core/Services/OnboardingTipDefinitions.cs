using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Static catalog of all first-time onboarding tips shown to new
/// operators during the <see cref="UserExperienceLevel.Beginner"/>
/// phase.
///
/// <para><b>Design rules for onboarding tips:</b></para>
/// <list type="bullet">
///   <item><see cref="TipDefinition.IsOneTime"/> = <c>true</c> —
///         the tip is permanently dismissed after the user closes
///         it. It never reappears, even across sessions.</item>
///   <item><see cref="TipDefinition.Level"/> =
///         <see cref="TipLevel.Beginner"/> — only visible while the
///         operator's experience profile is at Beginner level.</item>
///   <item><see cref="TipDefinition.Priority"/> = 90 — high priority
///         ensures onboarding tips always beat recurring guidance
///         tips in the single-banner slot.</item>
///   <item><see cref="TipDefinition.ContextCondition"/> — each tip
///         includes an appropriate mode/context guard so it only
///         appears in the correct operational context.</item>
/// </list>
///
/// <para><b>Auto-dismiss:</b> When the operator is promoted past
/// <see cref="UserExperienceLevel.Beginner"/> (either automatically
/// by <see cref="IOnboardingJourneyService"/> or manually by an
/// admin), <see cref="OnboardingTipRegistrar"/> bulk-dismisses all
/// remaining onboarding tips so they disappear silently.</para>
///
/// <para><b>Tip ID convention:</b>
/// <c>"Onboarding.{ViewName}.{TipName}"</c></para>
/// </summary>
public static class OnboardingTipDefinitions
{
    // ── Tip ID constants (used by registrar for bulk operations) ──

    public const string MainWorkspaceWelcome  = "Onboarding.MainWorkspace.Welcome";
    public const string SalesViewGetStarted   = "Onboarding.SalesView.GetStarted";
    public const string ProductsViewCatalog   = "Onboarding.ProductsView.Catalog";
    public const string BillingModeIntro      = "Onboarding.SalesView.BillingMode";
    public const string FirstTimeSetupDone    = "Onboarding.MainWorkspace.SetupComplete";
    public const string SettingsExplore       = "Onboarding.SystemSettings.Explore";

    /// <summary>
    /// All onboarding tip IDs. Used by <see cref="OnboardingTipRegistrar"/>
    /// to bulk-dismiss when the operator graduates past Beginner.
    /// </summary>
    public static IReadOnlyList<string> AllTipIds { get; } =
    [
        MainWorkspaceWelcome,
        SalesViewGetStarted,
        ProductsViewCatalog,
        BillingModeIntro,
        FirstTimeSetupDone,
        SettingsExplore,
    ];

    /// <summary>
    /// Returns all onboarding <see cref="TipDefinition"/>s.
    /// Called once at startup by <see cref="OnboardingTipRegistrar"/>.
    /// </summary>
    public static IReadOnlyList<TipDefinition> CreateAll() =>
    [
        // ── Dashboard / main workspace welcome ─────────────────
        new TipDefinition
        {
            TipId            = MainWorkspaceWelcome,
            WindowName       = "WorkspaceView",
            Title            = "Welcome to StoreAssistantPro",
            Message          = "This is your dashboard — use the sidebar to navigate between Products, Sales, and Settings.",
            ContextCondition = _ => true,
            Level            = TipLevel.Beginner,
            Priority         = 90,
            IsOneTime        = true,
        },

        // ── Setup complete acknowledgement ─────────────────────
        new TipDefinition
        {
            TipId            = FirstTimeSetupDone,
            WindowName       = "WorkspaceView",
            Title            = "Setup complete",
            Message          = "Your store is configured. Start by adding products to your catalog, then create your first sale.",
            ContextCondition = _ => true,
            Level            = TipLevel.Beginner,
            Priority         = 85,
            IsOneTime        = true,
        },

        // ── Sales view — getting started ───────────────────────
        new TipDefinition
        {
            TipId            = SalesViewGetStarted,
            WindowName       = "SalesView",
            Title            = "Sales history",
            Message          = "Your completed sales appear here. Switch to Billing mode (F8) to start a new transaction.",
            ContextCondition = ctx => ctx.OperationalMode == OperationalMode.Management,
            Level            = TipLevel.Beginner,
            Priority         = 90,
            IsOneTime        = true,
        },

        // ── Billing mode introduction ──────────────────────────
        new TipDefinition
        {
            TipId            = BillingModeIntro,
            WindowName       = "SalesView",
            Title            = "Billing mode",
            Message          = "You're in Billing mode — scan items or search products to build a cart, then press F5 to complete the sale.",
            ContextCondition = ctx => ctx.OperationalMode == OperationalMode.Billing,
            Level            = TipLevel.Beginner,
            Priority         = 90,
            IsOneTime        = true,
        },

        // ── Products view — catalog introduction ───────────────
        new TipDefinition
        {
            TipId            = ProductsViewCatalog,
            WindowName       = "ProductsView",
            Title            = "Product catalog",
            Message          = "Add your first product with Ctrl+N. Include a name, price, and tax category to get started.",
            ContextCondition = _ => true,
            Level            = TipLevel.Beginner,
            Priority         = 90,
            IsOneTime        = true,
        },

        // ── System settings — explore ──────────────────────────
        new TipDefinition
        {
            TipId            = SettingsExplore,
            WindowName       = "SettingsWindow",
            Title            = "System settings",
            Message          = "Configure your store name, tax rates, backup schedule, and user accounts from the sidebar.",
            ContextCondition = _ => true,
            Level            = TipLevel.Beginner,
            Priority         = 90,
            IsOneTime        = true,
        },
    ];
}
