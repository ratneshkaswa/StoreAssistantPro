using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that marks clickable card surfaces with a hand cursor.
/// In speed-first mode this intentionally avoids hover or press animation.
/// <example>
/// <code>&lt;Border Style="{StaticResource FluentCardStyle}" h:CardHover.IsEnabled="True"/&gt;</code>
/// </example>
/// </summary>
public static class CardHover
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(CardHover),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Border border) return;

        border.MouseEnter -= OnMouseEnter;
        border.MouseLeave -= OnMouseLeave;
        border.PreviewMouseLeftButtonDown -= OnMouseDown;
        border.PreviewMouseLeftButtonUp -= OnMouseUp;

        if ((bool)e.NewValue)
        {
            border.Cursor = Cursors.Hand;
        }
        else
        {
            if (Equals(border.Cursor, Cursors.Hand))
                border.ClearValue(FrameworkElement.CursorProperty);
        }
    }

    private static void OnMouseEnter(object sender, MouseEventArgs e) { }
    private static void OnMouseLeave(object sender, MouseEventArgs e) { }
    private static void OnMouseDown(object sender, MouseButtonEventArgs e) { }
    private static void OnMouseUp(object sender, MouseButtonEventArgs e) { }
}
