using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that applies conditional background highlights
/// to <see cref="DataGridRow"/> elements based on a bound
/// <see cref="RowHighlightLevel"/>.
/// <para>
/// <b>How it works:</b> Set <c>h:RowHighlight.Level</c> to a
/// <see cref="RowHighlightLevel"/> on each row (typically via a
/// <c>DataGrid.RowStyle</c> <c>DataTrigger</c> or direct binding).
/// The behavior resolves the matching <c>RowHighlight*</c> brush
/// from <c>DesignSystem.xaml</c> and sets the row's <c>Background</c>.
/// </para>
///
/// <para><b>Interaction with selection/hover:</b></para>
/// <para>
/// The highlight sets the <b>base</b> background. WPF's built-in
/// <c>IsSelected</c> and <c>IsMouseOver</c> triggers in
/// <c>GlobalStyles.xaml</c> have higher specificity and will
/// correctly overlay the highlight when the row is hovered or
/// selected. When the row is deselected, the highlight reappears.
/// </para>
///
/// <para><b>Performance:</b></para>
/// <list type="bullet">
///   <item>Single property set per row — no animations, no templates.</item>
///   <item>Brush resolved once per level change via <c>TryFindResource</c>.</item>
///   <item>Works with virtualized <c>DataGrid</c> rows — the behavior
///         fires on each row recycle.</item>
/// </list>
///
/// <para><b>Design tokens consumed:</b></para>
/// <list type="table">
///   <listheader><term>Level</term><description>Token key</description></listheader>
///   <item><term>Success</term><description><c>RowHighlightSuccess</c></description></item>
///   <item><term>Warning</term><description><c>RowHighlightWarning</c></description></item>
///   <item><term>Danger</term><description><c>RowHighlightDanger</c></description></item>
///   <item><term>Inactive</term><description><c>RowHighlightInactive</c></description></item>
/// </list>
///
/// <para><b>Usage (RowStyle DataTrigger):</b></para>
/// <code>
/// &lt;DataGrid.RowStyle&gt;
///     &lt;Style TargetType="DataGridRow" BasedOn="{StaticResource {x:Type DataGridRow}}"&gt;
///         &lt;Style.Triggers&gt;
///             &lt;DataTrigger Binding="{Binding StockStatus}" Value="LowStock"&gt;
///                 &lt;Setter Property="h:RowHighlight.Level" Value="Warning"/&gt;
///             &lt;/DataTrigger&gt;
///             &lt;DataTrigger Binding="{Binding StockStatus}" Value="OutOfStock"&gt;
///                 &lt;Setter Property="h:RowHighlight.Level" Value="Danger"/&gt;
///             &lt;/DataTrigger&gt;
///             &lt;DataTrigger Binding="{Binding IsActive}" Value="False"&gt;
///                 &lt;Setter Property="h:RowHighlight.Level" Value="Inactive"/&gt;
///             &lt;/DataTrigger&gt;
///         &lt;/Style.Triggers&gt;
///     &lt;/Style&gt;
/// &lt;/DataGrid.RowStyle&gt;
/// </code>
///
/// <para><b>Usage (direct binding):</b></para>
/// <code>
/// &lt;DataGrid.RowStyle&gt;
///     &lt;Style TargetType="DataGridRow" BasedOn="{StaticResource {x:Type DataGridRow}}"&gt;
///         &lt;Setter Property="h:RowHighlight.Level" Value="{Binding HighlightLevel}"/&gt;
///     &lt;/Style&gt;
/// &lt;/DataGrid.RowStyle&gt;
/// </code>
/// </summary>
public static class RowHighlight
{
    // ── Level attached property ──────────────────────────────────────

    /// <summary>
    /// The highlight level for a <see cref="DataGridRow"/>.
    /// Set to <see cref="RowHighlightLevel.None"/> (default) for no highlight.
    /// </summary>
    public static readonly DependencyProperty LevelProperty =
        DependencyProperty.RegisterAttached(
            "Level",
            typeof(RowHighlightLevel),
            typeof(RowHighlight),
            new FrameworkPropertyMetadata(
                RowHighlightLevel.None,
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnLevelChanged));

    public static RowHighlightLevel GetLevel(DependencyObject obj) =>
        (RowHighlightLevel)obj.GetValue(LevelProperty);

    public static void SetLevel(DependencyObject obj, RowHighlightLevel value) =>
        obj.SetValue(LevelProperty, value);

    // ── Original background storage ──────────────────────────────────

    /// <summary>
    /// Stores the row's original <c>Background</c> so it can be
    /// restored when the level returns to <c>None</c>.
    /// </summary>
    private static readonly DependencyProperty OriginalBackgroundProperty =
        DependencyProperty.RegisterAttached(
            "OriginalBackground",
            typeof(Brush),
            typeof(RowHighlight));

    /// <summary>Tracks whether we've captured the original background.</summary>
    private static readonly DependencyProperty HasCapturedProperty =
        DependencyProperty.RegisterAttached(
            "HasCaptured",
            typeof(bool),
            typeof(RowHighlight),
            new PropertyMetadata(false));

    // ── Level change handler ─────────────────────────────────────────

    private static void OnLevelChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGridRow row)
            return;

        var newLevel = (RowHighlightLevel)e.NewValue;

        // Capture original background on first highlight
        if (!(bool)row.GetValue(HasCapturedProperty))
        {
            row.SetValue(OriginalBackgroundProperty, row.Background);
            row.SetValue(HasCapturedProperty, true);
        }

        if (newLevel == RowHighlightLevel.None)
        {
            // Restore original background
            var original = row.GetValue(OriginalBackgroundProperty) as Brush;
            row.Background = original ?? Brushes.Transparent;
            return;
        }

        // Resolve the brush token
        var tokenKey = newLevel switch
        {
            RowHighlightLevel.Success => "RowHighlightSuccess",
            RowHighlightLevel.Warning => "RowHighlightWarning",
            RowHighlightLevel.Danger => "RowHighlightDanger",
            RowHighlightLevel.Inactive => "RowHighlightInactive",
            _ => null
        };

        if (tokenKey is not null && row.TryFindResource(tokenKey) is Brush brush)
        {
            row.Background = brush;
        }
    }

    // ── Token key resolver (internal for testing) ────────────────────

    /// <summary>
    /// Returns the DesignSystem.xaml resource key for a given level.
    /// Returns <c>null</c> for <see cref="RowHighlightLevel.None"/>.
    /// </summary>
    public static string? GetTokenKey(RowHighlightLevel level) =>
        level switch
        {
            RowHighlightLevel.Success => "RowHighlightSuccess",
            RowHighlightLevel.Warning => "RowHighlightWarning",
            RowHighlightLevel.Danger => "RowHighlightDanger",
            RowHighlightLevel.Inactive => "RowHighlightInactive",
            _ => null
        };
}
