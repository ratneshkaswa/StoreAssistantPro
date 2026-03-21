using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Shared validation micro-feedback for input controls.
/// Plays a short horizontal shake when a control transitions into
/// an invalid state, matching the Win11-style error affordance.
/// </summary>
public static class ValidationFeedback
{
    public static readonly DependencyProperty ShakeOnErrorProperty =
        DependencyProperty.RegisterAttached(
            "ShakeOnError",
            typeof(bool),
            typeof(ValidationFeedback),
            new PropertyMetadata(false, OnShakeOnErrorChanged));

    private static readonly DependencyProperty WatcherDescriptorProperty =
        DependencyProperty.RegisterAttached(
            "WatcherDescriptor",
            typeof(DependencyPropertyDescriptor),
            typeof(ValidationFeedback));

    private static readonly DependencyProperty WatcherHandlerProperty =
        DependencyProperty.RegisterAttached(
            "WatcherHandler",
            typeof(EventHandler),
            typeof(ValidationFeedback));

    private static readonly DependencyProperty LastHasErrorProperty =
        DependencyProperty.RegisterAttached(
            "LastHasError",
            typeof(bool),
            typeof(ValidationFeedback),
            new PropertyMetadata(false));

    public static bool GetShakeOnError(DependencyObject obj) =>
        (bool)obj.GetValue(ShakeOnErrorProperty);

    public static void SetShakeOnError(DependencyObject obj, bool value) =>
        obj.SetValue(ShakeOnErrorProperty, value);

    private static void OnShakeOnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        if ((bool)e.NewValue)
        {
            AttachWatcher(element);
            element.SetValue(LastHasErrorProperty, Validation.GetHasError(element));
            return;
        }

        DetachWatcher(element);
        element.ClearValue(LastHasErrorProperty);
    }

    private static void AttachWatcher(FrameworkElement element)
    {
        if (element.GetValue(WatcherHandlerProperty) is EventHandler)
            return;

        var descriptor =
            DependencyPropertyDescriptor.FromProperty(Validation.HasErrorProperty, element.GetType()) ??
            DependencyPropertyDescriptor.FromProperty(Validation.HasErrorProperty, typeof(FrameworkElement));

        if (descriptor is null)
            return;

        EventHandler handler = (_, _) => OnHasErrorChanged(element);
        descriptor.AddValueChanged(element, handler);

        element.SetValue(WatcherDescriptorProperty, descriptor);
        element.SetValue(WatcherHandlerProperty, handler);
    }

    private static void DetachWatcher(FrameworkElement element)
    {
        if (element.GetValue(WatcherDescriptorProperty) is not DependencyPropertyDescriptor descriptor ||
            element.GetValue(WatcherHandlerProperty) is not EventHandler handler)
        {
            return;
        }

        descriptor.RemoveValueChanged(element, handler);
        element.ClearValue(WatcherDescriptorProperty);
        element.ClearValue(WatcherHandlerProperty);
    }

    private static void OnHasErrorChanged(FrameworkElement element)
    {
        var hasError = Validation.GetHasError(element);
        var lastHasError = (bool)element.GetValue(LastHasErrorProperty);
        element.SetValue(LastHasErrorProperty, hasError);

        if (hasError && !lastHasError)
            PlayShake(element);
    }

    private static void PlayShake(FrameworkElement element)
    {
        var translate = EnsureTranslateTransform(element);
        var baseX = translate.X;
        var ease = ResolveEase(element, "FluentEaseDecelerate");

        translate.BeginAnimation(TranslateTransform.XProperty, null);

        var animation = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(300),
            FillBehavior = FillBehavior.Stop
        };

        animation.KeyFrames.Add(new EasingDoubleKeyFrame(baseX - 4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(50)), ease));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(baseX + 4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(110)), ease));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(baseX - 3, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(170)), ease));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(baseX + 3, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(230)), ease));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(baseX, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)), ease));

        translate.BeginAnimation(TranslateTransform.XProperty, animation);
    }

    private static TranslateTransform EnsureTranslateTransform(FrameworkElement element)
    {
        switch (element.RenderTransform)
        {
            case TranslateTransform translate:
                return translate;

            case TransformGroup group:
            {
                foreach (var child in group.Children)
                {
                    if (child is TranslateTransform existing)
                        return existing;
                }

                var appended = new TranslateTransform();
                group.Children.Add(appended);
                return appended;
            }

            case null:
            case MatrixTransform matrixTransform when matrixTransform.Matrix.IsIdentity:
            {
                var translate = new TranslateTransform();
                element.RenderTransform = translate;
                return translate;
            }

            default:
            {
                var group = new TransformGroup();
                group.Children.Add(element.RenderTransform);

                var translate = new TranslateTransform();
                group.Children.Add(translate);

                element.RenderTransform = group;
                return translate;
            }
        }
    }

    private static IEasingFunction? ResolveEase(FrameworkElement element, string resourceKey)
    {
        if (element.TryFindResource(resourceKey) is IEasingFunction easing)
            return easing;

        if (Application.Current?.TryFindResource(resourceKey) is IEasingFunction appEasing)
            return appEasing;

        return null;
    }
}
