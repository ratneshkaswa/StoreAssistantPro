using System.Windows;
using System.Windows.Controls.Primitives;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Retains the shared popup attached-property contract, but speed-first mode
/// disables popup open/close normalization work so flyouts rely on direct
/// platform rendering without extra event subscriptions.
/// </summary>
public static class PopupFlyoutMotion
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(PopupFlyoutMotion),
            new PropertyMetadata(false, OnNoOpChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    private static void OnNoOpChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Popup)
            return;

        // Intentionally no-op: speed-first mode avoids popup event hooks.
    }
}
