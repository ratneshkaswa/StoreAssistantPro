namespace StoreAssistantPro.Models;

/// <summary>
/// Determines the visual highlight applied to a <c>DataGridRow</c>
/// by the <c>h:RowHighlight</c> attached behavior.
/// <para>
/// Each level maps to a semantic background color token from
/// <c>DesignSystem.xaml</c>. The highlight is a subtle tint behind
/// the row — strong enough to scan visually, soft enough to not
/// interfere with text readability or selection highlighting.
/// </para>
///
/// <list type="table">
///   <listheader>
///     <term>Level</term>
///     <description>Usage / Color</description>
///   </listheader>
///   <item>
///     <term><see cref="None"/></term>
///     <description>Default — no highlight, standard row background.</description>
///   </item>
///   <item>
///     <term><see cref="Success"/></term>
///     <description>Positive state (in stock, active, confirmed).
///     Uses <c>RowHighlightSuccess</c> token.</description>
///   </item>
///   <item>
///     <term><see cref="Warning"/></term>
///     <description>Attention needed (low stock, pending, near expiry).
///     Uses <c>RowHighlightWarning</c> token.</description>
///   </item>
///   <item>
///     <term><see cref="Danger"/></term>
///     <description>Critical state (out of stock, overdue, error).
///     Uses <c>RowHighlightDanger</c> token.</description>
///   </item>
///   <item>
///     <term><see cref="Inactive"/></term>
///     <description>Dimmed / disabled row (inactive product, archived).
///     Uses <c>RowHighlightInactive</c> token.</description>
///   </item>
/// </list>
/// </summary>
public enum RowHighlightLevel
{
    /// <summary>No highlight — standard row background.</summary>
    None,

    /// <summary>Positive state (green tint).</summary>
    Success,

    /// <summary>Attention needed (amber tint).</summary>
    Warning,

    /// <summary>Critical state (red tint).</summary>
    Danger,

    /// <summary>Dimmed / disabled (grey tint).</summary>
    Inactive
}
