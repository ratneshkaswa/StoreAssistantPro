using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that auto-sets <see cref="AutomationProperties.NameProperty"/>
/// from a control's Content, Header, ToolTip, or Text when none is explicitly provided (#421).
/// <para>
/// Usage: <c>h:AccessibilityHelper.AutoLabel="True"</c> in XAML styles or individual elements.
/// </para>
/// </summary>
public static class AccessibilityHelper
{
    public static readonly DependencyProperty AutoLabelProperty =
        DependencyProperty.RegisterAttached(
            "AutoLabel",
            typeof(bool),
            typeof(AccessibilityHelper),
            new PropertyMetadata(false, OnAutoLabelChanged));

    public static bool GetAutoLabel(DependencyObject obj) => (bool)obj.GetValue(AutoLabelProperty);
    public static void SetAutoLabel(DependencyObject obj, bool value) => obj.SetValue(AutoLabelProperty, value);

    private static void OnAutoLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element || e.NewValue is not true)
            return;

        element.Loaded += static (sender, _) =>
        {
            if (sender is not FrameworkElement fe)
                return;

            // Skip if an explicit AutomationProperties.Name is already set
            var existing = AutomationProperties.GetName(fe);
            if (!string.IsNullOrEmpty(existing))
                return;

            var label = ResolveLabel(fe);
            if (!string.IsNullOrWhiteSpace(label))
                AutomationProperties.SetName(fe, label);
        };
    }

    private static string? ResolveLabel(FrameworkElement element)
    {
        // 1. ContentControl.Content (Button, Label, etc.)
        if (element is ContentControl { Content: string content })
            return content;

        // 2. HeaderedContentControl.Header (GroupBox, TabItem, etc.)
        if (element is HeaderedContentControl { Header: string header })
            return header;

        // 3. TextBox — use watermark/placeholder or Name
        if (element is TextBox tb)
            return Watermark.GetText(tb) ?? tb.Name;

        // 4. ToggleButton / CheckBox with Content
        if (element is ToggleButton { Content: string toggleContent })
            return toggleContent;

        // 5. ToolTip as fallback
        if (element.ToolTip is string tooltip)
            return tooltip;

        // 6. x:Name as last resort
        return string.IsNullOrEmpty(element.Name) ? null : element.Name;
    }
}
