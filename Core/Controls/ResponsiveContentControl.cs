using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// A <see cref="ContentControl"/> designed for the MainWindow content region
/// that stretches its content to fill available space while enabling vertical
/// scrolling when the content's desired height exceeds the viewport.
/// </summary>
public class ResponsiveContentControl : ContentControl
{
    private const string PartScrollViewer = "PART_ScrollViewer";

    public static readonly DependencyProperty VerticalScrollOffsetProperty =
        DependencyProperty.Register(
            nameof(VerticalScrollOffset),
            typeof(double),
            typeof(ResponsiveContentControl),
            new PropertyMetadata(0d));

    private ScrollViewer? _scrollViewer;

    static ResponsiveContentControl()
    {
        FocusableProperty.OverrideMetadata(
            typeof(ResponsiveContentControl),
            new FrameworkPropertyMetadata(false));
    }

    public double VerticalScrollOffset
    {
        get => (double)GetValue(VerticalScrollOffsetProperty);
        private set => SetValue(VerticalScrollOffsetProperty, value);
    }

    public event ScrollChangedEventHandler? ScrollOffsetChanged;

    protected override AutomationPeer OnCreateAutomationPeer() => new ResponsiveContentControlAutomationPeer(this);

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_scrollViewer is not null)
            _scrollViewer.ScrollChanged -= OnScrollViewerScrollChanged;

        _scrollViewer = GetTemplateChild(PartScrollViewer) as ScrollViewer;

        if (_scrollViewer is not null)
        {
            _scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
            VerticalScrollOffset = _scrollViewer.VerticalOffset;
        }
        else
        {
            VerticalScrollOffset = 0;
        }
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        _scrollViewer?.ScrollToTop();
        VerticalScrollOffset = 0;
    }

    private void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        VerticalScrollOffset = e.VerticalOffset;
        ScrollOffsetChanged?.Invoke(this, e);
    }

    private sealed class ResponsiveContentControlAutomationPeer(ResponsiveContentControl owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(ResponsiveContentControl);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
    }
}
