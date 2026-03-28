using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// A <see cref="ContentControl"/> designed for the MainWindow content region
/// that stretches its content to fill available space while enabling vertical
/// scrolling when the content's desired height exceeds the viewport.
/// <para>
/// Applies a fast opacity fade-in on content changes to eliminate the visual
/// flash that occurs when WPF tears down the old visual tree and builds the
/// new one from the DataTemplate.
/// </para>
/// </summary>
public class ResponsiveContentControl : ContentControl
{
    private const string PartScrollViewer = "PART_ScrollViewer";
    private static readonly Duration FadeInDuration = new(TimeSpan.FromMilliseconds(130));

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
        AutomationProperties.NameProperty.OverrideMetadata(
            typeof(ResponsiveContentControl),
            new FrameworkPropertyMetadata("Page content"));
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

        if (newContent is null)
            return;

        // Hide immediately so the empty/loading skeleton is never visible,
        // then fade in once WPF has built the new visual tree.
        Opacity = 0;
        BeginAnimation(OpacityProperty, null);
        Dispatcher.InvokeAsync(() =>
        {
            var fadeIn = new DoubleAnimation(0, 1, FadeInDuration)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            fadeIn.Freeze();
            BeginAnimation(OpacityProperty, fadeIn);
        }, DispatcherPriority.Loaded);
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

        protected override string GetNameCore()
        {
            var explicitName = base.GetNameCore();
            return string.IsNullOrWhiteSpace(explicitName) ? "Page content" : explicitName;
        }
    }
}
