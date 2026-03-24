using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Keeps focused inputs visible inside scrollable forms, which is especially
/// useful on touch devices when the on-screen keyboard reduces the viewport.
/// </summary>
public static class BringFocusedFieldIntoView
{
    private static readonly KeyboardFocusChangedEventHandler FocusHandler = OnGotKeyboardFocus;

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(BringFocusedFieldIntoView),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            element.AddHandler(Keyboard.GotKeyboardFocusEvent, FocusHandler, true);
        }
        else
        {
            element.RemoveHandler(Keyboard.GotKeyboardFocusEvent, FocusHandler);
        }
    }

    private static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not FrameworkElement host ||
            e.NewFocus is not FrameworkElement target ||
            !target.IsVisible)
        {
            return;
        }

        _ = host.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
        {
            if (!target.IsVisible)
            {
                return;
            }

            var width = Math.Max(target.ActualWidth, 1);
            var height = Math.Max(target.ActualHeight, 1);

            // Add a little headroom so focused fields do not sit directly at the viewport edge.
            target.BringIntoView(new Rect(-8, -24, width + 16, height + 48));
        }));
    }
}
