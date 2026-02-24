using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Central registry of all <see cref="TipDefinition"/>s in the
/// application. Modules register their tips at startup; the banner
/// system queries the registry at display time to find the
/// highest-priority eligible tip for a given window.
///
/// <para><b>Responsibilities:</b></para>
/// <list type="bullet">
///   <item>Stores all tip definitions in a thread-safe, append-only
///         collection keyed by <see cref="TipDefinition.WindowName"/>.</item>
///   <item>Filters tips by window name — only tips targeting the
///         requested window are considered.</item>
///   <item>Filters tips by <see cref="TipLevel"/> — tips above the
///         operator's current level are excluded.</item>
///   <item>Filters tips by <see cref="HelpContext"/> — each tip's
///         <see cref="TipDefinition.ContextCondition"/> predicate is
///         evaluated against the live context snapshot.</item>
///   <item>Filters tips by dismiss state — tips that the operator
///         has already dismissed (via <see cref="ITipStateService"/>)
///         are excluded.</item>
///   <item>Returns the single highest-priority surviving tip, or
///         <c>null</c> when no tip qualifies.</item>
/// </list>
///
/// <para><b>Lifetime:</b> Registered as a <b>singleton</b>. Tip
/// definitions are immutable records; registrations are append-only
/// and safe for concurrent reads after startup.</para>
///
/// <para><b>Usage — module registration (startup):</b></para>
/// <code>
/// registry.Register(new TipDefinition
/// {
///     TipId            = "SalesView.BillingShortcuts",
///     WindowName       = "SalesView",
///     Title            = "Billing shortcuts",
///     Message          = "Press F5 to start a new sale.",
///     ContextCondition = ctx => ctx.OperationalMode == OperationalMode.Billing,
///     Level            = TipLevel.Normal,
///     Priority         = 50,
///     IsOneTime        = false,
/// });
/// </code>
///
/// <para><b>Usage — banner resolution (display time):</b></para>
/// <code>
/// var tip = registry.Resolve("SalesView", TipLevel.Normal, helpContext);
/// if (tip is not null)
/// {
///     banner.Title   = tip.Title;
///     banner.TipText = tip.Message;
/// }
/// </code>
/// </summary>
public interface ITipRegistryService
{
    /// <summary>
    /// Adds a <see cref="TipDefinition"/> to the registry.
    /// Duplicate <see cref="TipDefinition.TipId"/> values are
    /// silently ignored (first-registration-wins).
    /// <para>
    /// Typically called once per tip during module startup
    /// (e.g. in <c>Add{Module}Module</c>).
    /// </para>
    /// </summary>
    void Register(TipDefinition tip);

    /// <summary>
    /// Adds multiple <see cref="TipDefinition"/>s in a single call.
    /// Convenience overload for modules that register a batch of tips.
    /// </summary>
    void Register(IEnumerable<TipDefinition> tips);

    /// <summary>
    /// Returns all registered tips that target the specified
    /// <paramref name="windowName"/>, unfiltered. Useful for
    /// diagnostics and the "Reset all tips" settings panel.
    /// </summary>
    IReadOnlyList<TipDefinition> GetTipsForWindow(string windowName);

    /// <summary>
    /// Returns all registered tips across every window.
    /// Useful for diagnostics, export, and the system settings UI.
    /// </summary>
    IReadOnlyList<TipDefinition> GetAll();

    /// <summary>
    /// Resolves the single highest-priority tip eligible for
    /// <paramref name="windowName"/> given the current operator
    /// level and application context. Returns <c>null</c> when
    /// no tip qualifies.
    ///
    /// <para><b>Filter pipeline (applied in order):</b></para>
    /// <list type="number">
    ///   <item>Window name match.</item>
    ///   <item><see cref="TipDefinition.Level"/> ≤
    ///         <paramref name="maxLevel"/>.</item>
    ///   <item><see cref="TipDefinition.ContextCondition"/> returns
    ///         <c>true</c> for <paramref name="context"/>.</item>
    ///   <item>Tip has not been dismissed (checked via
    ///         <see cref="ITipStateService"/>).</item>
    /// </list>
    /// <para>
    /// Surviving tips are ordered by
    /// <see cref="TipDefinition.Priority"/> descending; the first
    /// one wins.
    /// </para>
    /// </summary>
    /// <param name="windowName">
    /// Unqualified view/window name, e.g. <c>"SalesView"</c>.
    /// </param>
    /// <param name="maxLevel">
    /// Maximum <see cref="TipLevel"/> the current operator should
    /// see. Tips with <c>Level &gt; maxLevel</c> are excluded.
    /// </param>
    /// <param name="context">
    /// Current application context snapshot. Each tip's
    /// <see cref="TipDefinition.ContextCondition"/> is evaluated
    /// against this value.
    /// </param>
    /// <returns>
    /// The winning <see cref="TipDefinition"/>, or <c>null</c>
    /// when no tip passes all filters.
    /// </returns>
    TipDefinition? Resolve(string windowName, TipLevel maxLevel, HelpContext context);
}
