using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that enables consistent pixel-scrolling
/// on any <see cref="ScrollViewer"/>.
/// <para>
/// WPF's default <see cref="ScrollViewer"/> can scroll in logical item jumps.
/// This behavior intercepts <see cref="UIElement.PreviewMouseWheel"/> and
/// applies a direct pixel offset without animation.
/// </para>
/// <example>
/// <code>&lt;ScrollViewer h:SmoothScroll.IsEnabled="True"/&gt;</code>
/// </example>
/// </summary>
public static class SmoothScroll
{
    private const double PixelsPerScrollLine = 16;
    private const double MinimumScrollAmount = 32;

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(SmoothScroll),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static readonly DependencyProperty OriginalCanContentScrollProperty =
        DependencyProperty.RegisterAttached(
            "OriginalCanContentScroll", typeof(bool?), typeof(SmoothScroll),
            new PropertyMetadata(null));

    private static bool? GetOriginalCanContentScroll(DependencyObject obj) =>
        (bool?)obj.GetValue(OriginalCanContentScrollProperty);

    private static void SetOriginalCanContentScroll(DependencyObject obj, bool? value) =>
        obj.SetValue(OriginalCanContentScrollProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer sv) return;

        if ((bool)e.NewValue)
        {
            if (GetOriginalCanContentScroll(sv) is null)
                SetOriginalCanContentScroll(sv, sv.CanContentScroll);

            // Smooth scrolling expects pixel offsets. Logical item scrolling
            // makes wheel deltas jump too far on form-style views.
            sv.CanContentScroll = false;
            sv.PreviewMouseWheel += OnPreviewMouseWheel;
        }
        else
        {
            sv.PreviewMouseWheel -= OnPreviewMouseWheel;
            var original = GetOriginalCanContentScroll(sv);
            if (original.HasValue)
            {
                sv.CanContentScroll = original.Value;
                SetOriginalCanContentScroll(sv, null);
            }
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;

        // Let child selectors (e.g., ComboBox) consume wheel input so
        // dropdown scrolling and selection cycling work as expected.
        if (e.OriginalSource is DependencyObject source && IsInsideComboBox(source))
            return;

        // If wheel originates from a Popup tree (e.g., ComboBox dropdown),
        // do not hijack it with parent smooth scrolling.
        if (e.OriginalSource is DependencyObject popupSource &&
            FindVisualAncestor<System.Windows.Controls.Primitives.Popup>(popupSource) is not null)
            return;

        // If focus is currently inside a ComboBox (editable or dropdown state),
        // keep default wheel behavior to avoid broken selector scrolling.
        if (Keyboard.FocusedElement is DependencyObject focused && IsInsideComboBox(focused))
            return;

        // If any ComboBox dropdown is open in this window, never hijack wheel.
        // This prevents parent ScrollViewer animation from breaking dropdown list scrolling.
        var root = Window.GetWindow(sv) as DependencyObject ?? sv;
        if (HasOpenComboBox(root))
            return;

        e.Handled = true;

        var wheelAmount = GetWheelScrollAmount(sv, e.Delta);
        var delta = e.Delta > 0 ? -wheelAmount : wheelAmount;
        var newTarget = Math.Clamp(sv.VerticalOffset + delta, 0, sv.ScrollableHeight);
        sv.ScrollToVerticalOffset(newTarget);
    }

    private static double GetWheelScrollAmount(ScrollViewer sv, int mouseWheelDelta)
    {
        if (SystemParameters.WheelScrollLines < 0)
            return Math.Max(MinimumScrollAmount, Math.Min(sv.ViewportHeight * 0.85, sv.ScrollableHeight));

        var wheelSteps = Math.Max(1d, Math.Abs((double)mouseWheelDelta) / Mouse.MouseWheelDeltaForOneLine);
        var lineCount = Math.Max(1, SystemParameters.WheelScrollLines);
        var preferredAmount = lineCount * PixelsPerScrollLine * wheelSteps;
        var maximumAmount = Math.Max(MinimumScrollAmount, sv.ViewportHeight * 0.35);

        return Math.Clamp(preferredAmount, MinimumScrollAmount, maximumAmount);
    }

    private static bool IsInsideComboBox(DependencyObject start)
        => FindVisualAncestor<ComboBox>(start) is not null;

    private static T? FindVisualAncestor<T>(DependencyObject? current)
        where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
                return match;

            current = current switch
            {
                Visual or System.Windows.Media.Media3D.Visual3D => VisualTreeHelper.GetParent(current),
                _ => LogicalTreeHelper.GetParent(current)
            };
        }

        return null;
    }

    private static bool HasOpenComboBox(DependencyObject root)
    {
        if (root is ComboBox cb && cb.IsDropDownOpen)
            return true;

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (HasOpenComboBox(child))
                return true;
        }

        return false;
    }

}
