using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Normalizes popup flyouts so they open in a ready state without
/// entrance animation.
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
            new Action(() => ResetPopupChildState(popup)));
    }

    private static void OnPopupClosed(object? sender, EventArgs e)
    {
        if (sender is Popup popup)
            ResetPopupChildState(popup);
    }

    private static void ResetPopupChildState(Popup popup)
    {
        if (popup.Child is not FrameworkElement child)
            return;

        child.Opacity = 1;

        if (TryGetTranslateTransform(child) is TranslateTransform translate)
        {
            translate.X = 0;
            translate.Y = 0;
        }
    }

    private static TranslateTransform? TryGetTranslateTransform(FrameworkElement element) =>
        element.RenderTransform switch
        {
            TranslateTransform translate => translate,
            TransformGroup group => group.Children.OfType<TranslateTransform>().FirstOrDefault(),
            _ => null
        };
}
