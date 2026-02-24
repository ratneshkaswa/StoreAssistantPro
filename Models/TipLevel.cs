namespace StoreAssistantPro.Models;

/// <summary>
/// Classifies the expertise level of a <see cref="TipDefinition"/>.
/// Used by the adaptive tip system to filter guidance based on
/// the operator's experience — new cashiers see all levels while
/// experienced users only see <see cref="Advanced"/> tips.
///
/// <para><b>Ordering:</b> The underlying <see cref="int"/> value
/// increases with expertise so tips can be filtered with a simple
/// <c>tip.Level &lt;= userLevel</c> comparison.</para>
///
/// <para><b>Mapping guidance:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Level</term>
///     <description>When to assign</description>
///   </listheader>
///   <item>
///     <term><see cref="Beginner"/></term>
///     <description>Core workflows (scanning, cart, payment) —
///     essential for first-day operators.</description>
///   </item>
///   <item>
///     <term><see cref="Normal"/></term>
///     <description>Useful shortcuts, filter techniques, and
///     form best-practices — productive after a few sessions.</description>
///   </item>
///   <item>
///     <term><see cref="Advanced"/></term>
///     <description>Power features (bulk operations, keyboard-only
///     flows, advanced discount strategies) — experienced staff.</description>
///   </item>
/// </list>
/// </summary>
public enum TipLevel
{
    /// <summary>
    /// Essential tips shown to first-time or new operators.
    /// Covers core workflows like scanning, adding to cart,
    /// and completing a sale.
    /// </summary>
    Beginner = 0,

    /// <summary>
    /// Productivity tips shown after the operator is comfortable
    /// with the basics. Covers keyboard shortcuts, filters, and
    /// efficient form usage.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// Power-user tips for experienced operators. Covers bulk
    /// operations, advanced discount strategies, and keyboard-only
    /// workflows.
    /// </summary>
    Advanced = 2
}
