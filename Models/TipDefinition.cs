using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Models;

/// <summary>
/// Immutable definition of a single inline guidance tip.
///
/// <para>
/// <b>Purpose:</b> Provides all metadata the adaptive tip system
/// needs to decide <em>which</em> tip to show, <em>where</em> to
/// show it, and <em>whether</em> the current context applies —
/// without embedding any of that logic in view code or XAML.
/// </para>
///
/// <para><b>Lifecycle:</b></para>
/// <list type="number">
///   <item><b>Registration</b> — Each module registers its
///         <see cref="TipDefinition"/>s at startup (typically in
///         <c>Add{Module}Module</c> via DI).</item>
///   <item><b>Selection</b> — The tip service evaluates
///         <see cref="ContextCondition"/> against the current
///         <see cref="HelpContext"/> to determine which tips are
///         eligible for the active window / view.</item>
///   <item><b>Ordering</b> — Eligible tips are sorted by
///         <see cref="Priority"/> (descending) so the most
///         important tip wins the single banner slot.</item>
///   <item><b>Display</b> — The winning tip's <see cref="Title"/>
///         and <see cref="Message"/> are set on the
///         <c>InlineTipBanner</c> via
///         <c>TipBannerAutoState</c>.</item>
///   <item><b>Dismissal</b> — When the user closes the banner,
///         <see cref="ITipStateService.DismissTip"/> records
///         <see cref="TipId"/>. If <see cref="IsOneTime"/> is
///         <c>true</c>, the tip never reappears; otherwise it
///         reappears next session.</item>
/// </list>
///
/// <para><b>ContextCondition predicate:</b></para>
/// <para>
/// The <see cref="ContextCondition"/> delegate receives the
/// current <see cref="HelpContext"/> snapshot and returns
/// <c>true</c> when the tip is relevant. This allows arbitrarily
/// complex matching without requiring new enum values or flag
/// combinations:
/// </para>
/// <code>
/// // Billing-mode only tip
/// ContextCondition = ctx => ctx.OperationalMode == OperationalMode.Billing
///
/// // Offline + Billing
/// ContextCondition = ctx => ctx.IsOfflineMode
///                        &amp;&amp; ctx.OperationalMode == OperationalMode.Billing
///
/// // Admin-only tip in any mode
/// ContextCondition = ctx => ctx.UserRole == UserType.Admin
///
/// // Always shown (context-agnostic)
/// ContextCondition = _ => true
/// </code>
///
/// <para><b>Example registration:</b></para>
/// <code>
/// new TipDefinition
/// {
///     TipId         = "SalesView.BillingShortcuts",
///     WindowName    = "SalesView",
///     Title         = "Billing shortcuts",
///     Message       = "Press F5 to start a new sale, F8 to toggle billing mode.",
///     ContextCondition = ctx => ctx.OperationalMode == OperationalMode.Billing,
///     Level         = TipLevel.Normal,
///     Priority      = 50,
///     IsOneTime     = false,
/// }
/// </code>
/// </summary>
public sealed record TipDefinition
{
    /// <summary>
    /// Globally unique identifier for this tip, used as the
    /// persistence key in <see cref="ITipStateService"/>.
    /// <para>
    /// Convention: <c>"{ViewName}.{TipName}"</c>, e.g.
    /// <c>"SalesView.BillingShortcuts"</c>,
    /// <c>"ProductsView.QuickAdd"</c>.
    /// </para>
    /// </summary>
    public required string TipId { get; init; }

    /// <summary>
    /// Name of the window or view where the tip should appear.
    /// Matches the view's unqualified type name, e.g.
    /// <c>"SalesView"</c>, <c>"FirmManagementWindow"</c>.
    /// <para>
    /// Used by the tip service to scope eligible tips to the
    /// currently active window. Multiple tips can target the
    /// same window — <see cref="Priority"/> determines which one
    /// wins the single banner slot.
    /// </para>
    /// </summary>
    public required string WindowName { get; init; }

    /// <summary>
    /// Bold title text displayed after the bulb icon on the
    /// <c>InlineTipBanner</c>. Keep short — one to three words.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Descriptive body text displayed below the title.
    /// Should be a single sentence that provides actionable guidance.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Predicate evaluated against the current <see cref="HelpContext"/>
    /// to determine whether the tip is relevant right now.
    /// <para>
    /// Return <c>true</c> to make the tip eligible for display;
    /// <c>false</c> to suppress it. The predicate is re-evaluated
    /// on every context change (mode switch, offline transition,
    /// focus-lock change) so tips adapt in real-time.
    /// </para>
    /// <para>
    /// Use <c>_ => true</c> for tips that are always applicable
    /// regardless of context.
    /// </para>
    /// </summary>
    public required Func<HelpContext, bool> ContextCondition { get; init; }

    /// <summary>
    /// Expertise level the tip targets. The adaptive tip system
    /// compares this against the operator's current level to decide
    /// whether the tip should be shown.
    /// <para>
    /// A <see cref="TipLevel.Beginner"/> tip is shown to all
    /// operators; a <see cref="TipLevel.Advanced"/> tip is shown
    /// only to operators whose level is ≥ Advanced.
    /// </para>
    /// </summary>
    public TipLevel Level { get; init; } = TipLevel.Normal;

    /// <summary>
    /// Display priority (higher = more important). When multiple
    /// tips are eligible for the same window, the one with the
    /// highest <see cref="Priority"/> wins the single banner slot.
    /// <para>
    /// Suggested scale:
    /// <list type="bullet">
    ///   <item><b>0–29</b> — Low: nice-to-know, background guidance.</item>
    ///   <item><b>30–69</b> — Normal: workflow tips, shortcuts.</item>
    ///   <item><b>70–100</b> — High: safety warnings, data-loss
    ///         prevention, offline mode notices.</item>
    /// </list>
    /// </para>
    /// </summary>
    public int Priority { get; init; } = 50;

    /// <summary>
    /// When <c>true</c>, the tip is permanently dismissed after the
    /// user closes it — it never reappears even across sessions.
    /// When <c>false</c> (default), the tip reappears on the next
    /// application session after the dismiss state is cleared.
    /// <para>
    /// Use <c>true</c> for onboarding tips that are only useful
    /// once (e.g. "Welcome to billing mode"). Use <c>false</c> for
    /// recurring reminders (e.g. "Offline: changes will sync later").
    /// </para>
    /// </summary>
    public bool IsOneTime { get; init; }
}
