using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Single skeleton placeholder row that displays a pulsing shimmer
/// bar mimicking a loading data row.
/// <para>
/// <b>Visual:</b> A rounded rectangle with a soft gradient sweep
/// that conveys "content is loading" without blocking the UI thread.
/// </para>
/// <para>
/// Typically used inside <see cref="SkeletonGrid"/> but can be placed
/// standalone for custom loading layouts. Set <see cref="WidthFraction"/>
/// to vary bar lengths across rows for a natural staggered look.
/// </para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;controls:SkeletonRow WidthFraction="0.75"/&gt;
/// </code>
/// </summary>
public class SkeletonRow : Control
{
    // ── WidthFraction DP ──────────────────────────────────────────

    /// <summary>
    /// Fraction of container width to fill (0.0 to 1.0).
    /// Translated to a percentage-based <c>MaxWidth</c> via the
    /// style's <c>HorizontalAlignment="Stretch"</c> approach.
    /// Default: 1.0 (full width).
    /// </summary>
    public static readonly DependencyProperty WidthFractionProperty =
        DependencyProperty.Register(
            nameof(WidthFraction), typeof(double), typeof(SkeletonRow),
            new PropertyMetadata(1.0));

    public double WidthFraction
    {
        get => (double)GetValue(WidthFractionProperty);
        set => SetValue(WidthFractionProperty, value);
    }

    // ── Constructor ───────────────────────────────────────────────

    static SkeletonRow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SkeletonRow),
            new FrameworkPropertyMetadata(typeof(SkeletonRow)));

        FocusableProperty.OverrideMetadata(
            typeof(SkeletonRow),
            new FrameworkPropertyMetadata(false));

        IsHitTestVisibleProperty.OverrideMetadata(
            typeof(SkeletonRow),
            new FrameworkPropertyMetadata(false));
    }
}
