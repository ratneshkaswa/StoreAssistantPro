using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Pure-logic rule engine that evaluates a <see cref="FocusContext"/>
/// against the <see cref="IFocusMapRegistry"/> and produces the
/// optimal <see cref="FocusHint"/> for the given situation.
/// <para>
/// <b>Context-aware rules:</b>
/// </para>
/// <code>
///   ┌───────────────┬──────────────┬───────────────────────────────┐
///   │ Mode          │ Context Type │ Landing Target                │
///   ├───────────────┼──────────────┼───────────────────────────────┤
///   │ Management    │ Page         │ SearchInput or PrimaryInput   │
///   │ Billing       │ Page         │ Named("BillingSearchBox")     │
///   │ *             │ Dialog       │ PrimaryInput                  │
///   │ *             │ Form         │ PrimaryInput                  │
///   │ * (no map)    │ *            │ FirstInput (fallback)         │
///   └───────────────┴──────────────┴───────────────────────────────┘
/// </code>
///
/// <para><b>Architecture rules:</b></para>
/// <list type="bullet">
///   <item>Registered as a <b>singleton</b>.</item>
///   <item>No WPF types — pure logic, fully unit-testable.</item>
///   <item>Called by <see cref="IPredictiveFocusService"/> on every
///         workflow transition instead of hardcoded decisions.</item>
///   <item>Returns a <see cref="FocusHint"/> — never touches UI.</item>
/// </list>
/// </summary>
public interface IFocusRuleEngine
{
    /// <summary>
    /// Evaluate the context and produce the best focus hint.
    /// </summary>
    /// <param name="context">Current application state snapshot.</param>
    /// <param name="reason">Diagnostic label for the produced hint.</param>
    /// <param name="basePriority">Base priority for the produced hint.</param>
    FocusHint Evaluate(FocusContext context, string reason, int basePriority = 5);
}
