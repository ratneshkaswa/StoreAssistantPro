using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Singleton implementation of <see cref="IFocusRuleEngine"/>.
/// <para>
/// Applies a deterministic priority chain to resolve the best landing
/// target for any combination of operational mode, context type, and
/// registered <see cref="FocusMap"/>. The rules never touch the visual
/// tree — they return pure data (<see cref="FocusHint"/>) that XAML
/// behaviors execute.
/// </para>
///
/// <para><b>Rule evaluation order (highest priority wins):</b></para>
/// <list type="number">
///   <item><b>Billing mode + page context</b> → always land on
///         <c>BillingSearchBox</c> (product entry is king).</item>
///   <item><b>Dialog context</b> → land on <c>PrimaryInput</c> role
///         from the dialog's <see cref="FocusMap"/>, or <c>FirstInput</c>
///         if no map is registered.</item>
///   <item><b>Form context</b> → land on <c>PrimaryInput</c> role
///         from the form's <see cref="FocusMap"/>, or <c>FirstInput</c>.</item>
///   <item><b>Management mode + page context</b> → land on
///         <c>SearchInput</c> role (if the page has a search box), then
///         fall back to <c>PrimaryInput</c>, then <c>FirstInput</c>.</item>
///   <item><b>No map registered</b> → <c>FirstInput</c> (generic
///         WPF focus traversal).</item>
/// </list>
/// </summary>
public sealed class FocusRuleEngine : IFocusRuleEngine
{
    private readonly IFocusMapRegistry _registry;

    /// <summary>Billing search box — the sacred landing target.</summary>
    private const string BillingSearchBox = "BillingSearchBox";

    public FocusRuleEngine(IFocusMapRegistry registry)
    {
        _registry = registry;
    }

    /// <inheritdoc/>
    public FocusHint Evaluate(FocusContext context, string reason, int basePriority = 5)
    {
        ArgumentNullException.ThrowIfNull(context);

        // ── Rule 1: Billing mode + page → always BillingSearchBox ──
        if (context.Mode == OperationalMode.Billing
            && context.ContextType == FocusContextType.Page)
        {
            return FocusHint.Named(BillingSearchBox, reason, basePriority + 15);
        }

        // ── Attempt to resolve from the FocusMap registry ──
        var map = string.IsNullOrWhiteSpace(context.ContextKey)
            ? null
            : _registry.Get(context.ContextKey);

        // ── Rule 2: Dialog → PrimaryInput from map ──
        if (context.ContextType == FocusContextType.Dialog)
        {
            return ResolveFromMap(map, FocusRole.PrimaryInput, reason, basePriority + 10);
        }

        // ── Rule 3: Form → PrimaryInput from map ──
        if (context.ContextType == FocusContextType.Form)
        {
            return ResolveFromMap(map, FocusRole.PrimaryInput, reason, basePriority + 10);
        }

        // ── Rule 4: Management mode + page → SearchInput → PrimaryInput ──
        if (context.Mode == OperationalMode.Management
            && context.ContextType == FocusContextType.Page)
        {
            if (map is not null)
            {
                // Prefer SearchInput for management pages (search-centric UX)
                var search = map.GetByRole(FocusRole.SearchInput);
                if (search is not null)
                    return FocusHint.Named(search.ElementName, reason, basePriority);

                // Fall back to PrimaryInput
                var primary = map.GetByRole(FocusRole.PrimaryInput);
                if (primary is not null)
                    return FocusHint.Named(primary.ElementName, reason, basePriority);

                // Fall back to first entry in the map
                var landing = map.GetLandingTarget();
                if (landing is not null)
                    return FocusHint.Named(landing, reason, basePriority);
            }
        }

        // ── Rule 5: No map or no matching role → FirstInput ──
        return FocusHint.FirstInput(reason, basePriority);
    }

    /// <summary>
    /// Resolves a named hint from a FocusMap by role, falling back to
    /// the landing target, then to <c>FirstInput</c>.
    /// </summary>
    private static FocusHint ResolveFromMap(
        FocusMap? map, FocusRole preferredRole, string reason, int priority)
    {
        if (map is null)
            return FocusHint.FirstInput(reason, priority);

        var entry = map.GetByRole(preferredRole);
        if (entry is not null)
            return FocusHint.Named(entry.ElementName, reason, priority);

        var landing = map.GetLandingTarget();
        if (landing is not null)
            return FocusHint.Named(landing, reason, priority);

        return FocusHint.FirstInput(reason, priority);
    }
}
