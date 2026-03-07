using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that enables Windows 11-style pixel-smooth scrolling
/// on any <see cref="ScrollViewer"/>.
/// <para>
/// WPF's default <see cref="ScrollViewer"/> scrolls in discrete line jumps.
/// This behavior intercepts <see cref="UIElement.PreviewMouseWheel"/> and
/// animates <see cref="ScrollViewer.VerticalOffset"/> with a decelerate
/// easing function for buttery-smooth scrolling.
/// </para>
/// <example>
/// <code>&lt;ScrollViewer h:SmoothScroll.IsEnabled="True"/&gt;</code>
/// </example>
/// </summary>
public static class SmoothScroll
{
    private const double ScrollAmount = 80;
    private static readonly Duration AnimationDuration = new(TimeSpan.FromMilliseconds(200));
    private static readonly IEasingFunction Easing = new CubicEase { EasingMode = EasingMode.EaseOut };

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(SmoothScroll),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static readonly DependencyProperty TargetOffsetProperty =
        DependencyProperty.RegisterAttached(
            "TargetOffset", typeof(double), typeof(SmoothScroll),
            new PropertyMetadata(0.0));

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer sv) return;

        if ((bool)e.NewValue)
            sv.PreviewMouseWheel += OnPreviewMouseWheel;
        else
            sv.PreviewMouseWheel -= OnPreviewMouseWheel;
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;

        e.Handled = true;

        var currentTarget = (double)sv.GetValue(TargetOffsetProperty);

        // If animation hasn't started yet or offset was manually changed, sync
        if (Math.Abs(currentTarget - sv.VerticalOffset) > ScrollAmount * 2)
            currentTarget = sv.VerticalOffset;

        var delta = e.Delta > 0 ? -ScrollAmount : ScrollAmount;
        var newTarget = Math.Clamp(currentTarget + delta, 0, sv.ScrollableHeight);

        sv.SetValue(TargetOffsetProperty, newTarget);

        var animation = new DoubleAnimation
        {
            To = newTarget,
            Duration = AnimationDuration,
            EasingFunction = Easing
        };
        animation.Freeze();

        sv.BeginAnimation(ScrollViewerOffsetMediator.VerticalOffsetProperty, null);

        // Use a mediator to animate ScrollViewer.VerticalOffset (read-only DP)
        var mediator = GetOrCreateMediator(sv);
        mediator.BeginAnimation(ScrollViewerOffsetMediator.VerticalOffsetProperty, animation);
    }

    private static ScrollViewerOffsetMediator GetOrCreateMediator(ScrollViewer sv)
    {
        if (sv.Tag is ScrollViewerOffsetMediator existing)
            return existing;

        var mediator = new ScrollViewerOffsetMediator(sv);
        sv.Tag = mediator;
        return mediator;
    }
}

/// <summary>
/// Animatable proxy for <see cref="ScrollViewer.VerticalOffset"/> which is
/// a read-only dependency property and cannot be directly animated.
/// </summary>
internal sealed class ScrollViewerOffsetMediator : Animatable
{
    private readonly ScrollViewer _scrollViewer;

    public static readonly DependencyProperty VerticalOffsetProperty =
        DependencyProperty.Register(
            nameof(VerticalOffset), typeof(double), typeof(ScrollViewerOffsetMediator),
            new PropertyMetadata(0.0, OnVerticalOffsetChanged));

    public double VerticalOffset
    {
        get => (double)GetValue(VerticalOffsetProperty);
        set => SetValue(VerticalOffsetProperty, value);
    }

    public ScrollViewerOffsetMediator(ScrollViewer scrollViewer)
    {
        _scrollViewer = scrollViewer;
    }

    private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewerOffsetMediator mediator)
            mediator._scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
    }

    protected override Freezable CreateInstanceCore() => new ScrollViewerOffsetMediator(_scrollViewer);
}
