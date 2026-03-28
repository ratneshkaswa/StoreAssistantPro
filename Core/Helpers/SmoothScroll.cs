using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Retains the shared attached-property contract for historical XAML usage,
/// but speed-first mode disables custom wheel interception entirely so the
/// platform ScrollViewer behavior runs without extra handler overhead.
/// </summary>
public static class SmoothScroll
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(SmoothScroll),
            new PropertyMetadata(false, OnNoOpChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnNoOpChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer)
            return;

        // Intentionally no-op: speed-first mode leaves scrolling fully native.
    }
}
