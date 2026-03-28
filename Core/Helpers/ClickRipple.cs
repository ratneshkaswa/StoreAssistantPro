using System.Windows;
namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Retains the shared attached-property contract for button surfaces,
/// but speed-first mode disables decorative click ripple visuals.
/// </summary>
public static class ClickRipple
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ClickRipple),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        _ = element;
        _ = e;

        // Intentionally no-op: speed-first mode disables decorative click ripples
        // and avoids attaching any mouse handlers for them.
    }
}
