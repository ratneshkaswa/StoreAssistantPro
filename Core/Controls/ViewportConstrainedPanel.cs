using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// A single-child panel placed inside a <see cref="ScrollViewer"/> that measures
/// its child against the viewport dimensions rather than infinite space.
/// <para>
/// When the child fits within the viewport the child is arranged at the full
/// viewport size (stretch behaviour — star-sized Grid rows work correctly).
/// When the child's desired size exceeds the viewport the panel returns the
/// larger desired size, which causes the parent <see cref="ScrollViewer"/> to
/// display scrollbars.
/// </para>
/// </summary>
public sealed class ViewportConstrainedPanel : Panel
{
    public static readonly DependencyProperty ViewportWidthProperty =
        DependencyProperty.Register(
            nameof(ViewportWidth),
            typeof(double),
            typeof(ViewportConstrainedPanel),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty ViewportHeightProperty =
        DependencyProperty.Register(
            nameof(ViewportHeight),
            typeof(double),
            typeof(ViewportConstrainedPanel),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double ViewportWidth
    {
        get => (double)GetValue(ViewportWidthProperty);
        set => SetValue(ViewportWidthProperty, value);
    }

    public double ViewportHeight
    {
        get => (double)GetValue(ViewportHeightProperty);
        set => SetValue(ViewportHeightProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (InternalChildren.Count == 0)
            return default;

        var child = InternalChildren[0];

        // Use the real viewport dimensions so the child sees a finite constraint,
        // preserving star-sized Grid rows/columns inside the hosted UserControl.
        // Guard against Infinity: when the viewport hasn't been measured yet AND
        // the ScrollViewer passes Infinity, clamp to zero so MeasureOverride
        // never returns PositiveInfinity (which WPF forbids).
        var width = ViewportWidth > 0
            ? ViewportWidth
            : (double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width);
        var height = ViewportHeight > 0
            ? ViewportHeight
            : (double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);

        child.Measure(new Size(width, height));

        // If the child needs more space than the viewport, return its full desired
        // size so the parent ScrollViewer creates scrollbars.
        return new Size(
            Math.Max(width, child.DesiredSize.Width),
            Math.Max(height, child.DesiredSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (InternalChildren.Count > 0)
            InternalChildren[0].Arrange(new Rect(finalSize));

        return finalSize;
    }
}
