using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Shared validation micro-feedback for input controls.
/// In speed-first mode this keeps the validation watcher and layout-safe
/// transform path but avoids animated shake.
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
        translate.X = translate.X;
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

}
