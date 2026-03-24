using System.Windows;
using System.Windows.Input;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Tracks a lightweight pressed state for non-button elements so shared
/// styles can apply Win11 rest/hover/pressed layers consistently.
/// </summary>
public static class PressStateTracker
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(PressStateTracker),
            new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyPropertyKey IsPressedPropertyKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "IsPressed",
            typeof(bool),
            typeof(PressStateTracker),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    public static readonly DependencyProperty IsPressedProperty = IsPressedPropertyKey.DependencyProperty;

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static bool GetIsPressed(DependencyObject obj) => (bool)obj.GetValue(IsPressedProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            element.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            element.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            element.MouseLeave += OnMouseLeave;
            element.LostMouseCapture += OnLostMouseCapture;
        }
        else
        {
            element.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            element.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
            element.MouseLeave -= OnMouseLeave;
            element.LostMouseCapture -= OnLostMouseCapture;
            element.ClearValue(IsPressedPropertyKey.DependencyProperty);
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is UIElement element && element.IsEnabled)
        {
            element.SetValue(IsPressedPropertyKey, true);
        }
    }

    private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is UIElement element)
        {
            element.SetValue(IsPressedPropertyKey, false);
        }
    }

    private static void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is UIElement element)
        {
            element.SetValue(IsPressedPropertyKey, false);
        }
    }

    private static void OnLostMouseCapture(object sender, MouseEventArgs e)
    {
        if (sender is UIElement element)
        {
            element.SetValue(IsPressedPropertyKey, false);
        }
    }
}
