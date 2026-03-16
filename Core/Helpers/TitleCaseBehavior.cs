using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that auto-capitalizes the first letter of each word
/// when a TextBox loses focus. Ported from ShopManagement.
/// </summary>
public static class TitleCaseBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(TitleCaseBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox tb) return;

        if ((bool)e.NewValue)
            tb.LostFocus += OnLostFocus;
        else
            tb.LostFocus -= OnLostFocus;
    }

    private static void OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb || string.IsNullOrWhiteSpace(tb.Text)) return;

        var titleCase = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tb.Text.ToLower(CultureInfo.CurrentCulture));
        if (tb.Text != titleCase)
            tb.Text = titleCase;
    }
}
