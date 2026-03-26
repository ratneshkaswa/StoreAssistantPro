using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// A single-child panel placed inside a <see cref="ScrollViewer"/> that measures
/// its child against the viewport dimensions rather than infinite space.
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

    protected override AutomationPeer OnCreateAutomationPeer() => new ViewportConstrainedPanelAutomationPeer(this);

    protected override Size MeasureOverride(Size availableSize)
    {
        if (InternalChildren.Count == 0)
            return default;

        var child = InternalChildren[0];
        var width = ViewportWidth > 0
            ? ViewportWidth
            : (double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width);
        var height = ViewportHeight > 0
            ? ViewportHeight
            : (double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);

        child.Measure(new Size(width, height));

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

    private sealed class ViewportConstrainedPanelAutomationPeer(ViewportConstrainedPanel owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(ViewportConstrainedPanel);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

        protected override string GetNameCore()
        {
            var explicitName = base.GetNameCore();
            return string.IsNullOrWhiteSpace(explicitName) ? "Viewport content host" : explicitName;
        }
    }
}
