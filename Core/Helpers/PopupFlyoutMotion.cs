using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Applies a short anchored slide + fade entrance to popup flyouts.
/// The popup child animates from the anchor point instead of snapping open.
/// </summary>
public static class PopupFlyoutMotion
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(PopupFlyoutMotion),
            new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty IsSubscribedProperty =
        DependencyProperty.RegisterAttached(
            "IsSubscribed",
            typeof(bool),
            typeof(PopupFlyoutMotion),
            new PropertyMetadata(false));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Popup popup || e.NewValue is not true || popup.GetValue(IsSubscribedProperty) is true)
            return;

        popup.SetValue(IsSubscribedProperty, true);
        popup.Opened += OnPopupOpened;
        popup.Closed += OnPopupClosed;
    }

    private static void OnPopupOpened(object? sender, EventArgs e)
    {
        if (sender is not Popup popup)
            return;

        popup.Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() => AnimatePopupChild(popup)));
    }

    private static void OnPopupClosed(object? sender, EventArgs e)
    {
        if (sender is not Popup popup || popup.Child is not FrameworkElement child)
            return;

        child.BeginAnimation(UIElement.OpacityProperty, null);

        if (TryGetTranslateTransform(child) is TranslateTransform translate)
        {
            translate.BeginAnimation(TranslateTransform.XProperty, null);
            translate.BeginAnimation(TranslateTransform.YProperty, null);
            translate.X = 0;
            translate.Y = 0;
        }

        child.Opacity = 1;
    }

    private static void AnimatePopupChild(Popup popup)
    {
        if (popup.Child is not FrameworkElement child)
            return;

        var duration = ResolveDuration(child, "FluentDurationNormal", TimeSpan.FromMilliseconds(120));
        if (duration == TimeSpan.Zero)
        {
            child.Opacity = 1;
            return;
        }

        var ease = child.TryFindResource("FluentEaseDecelerate") as IEasingFunction;
        var offset = ResolveDouble(child, "MotionFlyoutOffset", 12);
        var (startX, startY) = ResolveOriginOffset(popup, offset);

        EnsureTranslateTransform(child);
        child.BeginAnimation(UIElement.OpacityProperty, null);
        child.Opacity = 0;

        var fadeAnimation = new DoubleAnimation(0, 1, new Duration(duration))
        {
            EasingFunction = ease
        };
        fadeAnimation.Freeze();
        child.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);

        if (TryGetTranslateTransform(child) is not TranslateTransform translate)
            return;

        translate.BeginAnimation(TranslateTransform.XProperty, null);
        translate.BeginAnimation(TranslateTransform.YProperty, null);
        translate.X = startX;
        translate.Y = startY;

        if (Math.Abs(startX) > 0.01)
        {
            var xAnimation = new DoubleAnimation(startX, 0, new Duration(duration))
            {
                EasingFunction = ease
            };
            xAnimation.Freeze();
            translate.BeginAnimation(TranslateTransform.XProperty, xAnimation);
        }

        if (Math.Abs(startY) > 0.01)
        {
            var yAnimation = new DoubleAnimation(startY, 0, new Duration(duration))
            {
                EasingFunction = ease
            };
            yAnimation.Freeze();
            translate.BeginAnimation(TranslateTransform.YProperty, yAnimation);
        }
    }

    private static (double X, double Y) ResolveOriginOffset(Popup popup, double offset) =>
        popup.Placement switch
        {
            PlacementMode.Right => (-offset, 0),
            PlacementMode.Left => (offset, 0),
            PlacementMode.Top => (0, offset),
            PlacementMode.Bottom => (0, -offset),
            PlacementMode.AbsolutePoint => (0, -offset),
            PlacementMode.RelativePoint => (0, -offset),
            PlacementMode.MousePoint => (0, -offset),
            _ => (0, -offset)
        };

    private static TimeSpan ResolveDuration(FrameworkElement element, string key, TimeSpan fallback)
    {
        if (element.TryFindResource(key) is Duration duration && duration.HasTimeSpan)
            return duration.TimeSpan;

        return fallback;
    }

    private static double ResolveDouble(FrameworkElement element, string key, double fallback) =>
        element.TryFindResource(key) is double value ? value : fallback;

    private static void EnsureTranslateTransform(FrameworkElement element)
    {
        if (element.RenderTransform is TranslateTransform)
            return;

        if (element.RenderTransform is null ||
            element.RenderTransform is MatrixTransform matrix && matrix.Matrix.IsIdentity)
        {
            element.RenderTransform = new TranslateTransform();
            return;
        }

        var group = new TransformGroup();
        group.Children.Add(element.RenderTransform);
        group.Children.Add(new TranslateTransform());
        element.RenderTransform = group;
    }

    private static TranslateTransform? TryGetTranslateTransform(FrameworkElement element) =>
        element.RenderTransform switch
        {
            TranslateTransform translate => translate,
            TransformGroup group => group.Children.OfType<TranslateTransform>().FirstOrDefault(),
            _ => null
        };
}
