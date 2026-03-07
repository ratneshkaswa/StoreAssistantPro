using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Windows 11 WinUI-style Expander with animated expand/collapse.
/// Displays a header with a chevron indicator that toggles a content area.
/// <example>
/// <code>
/// &lt;controls:FluentExpander Header="Advanced Options"&gt;
///     &lt;StackPanel&gt;...&lt;/StackPanel&gt;
/// &lt;/controls:FluentExpander&gt;
/// </code>
/// </example>
/// </summary>
public class FluentExpander : HeaderedContentControl
{
    static FluentExpander()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(FluentExpander), new FrameworkPropertyMetadata(typeof(FluentExpander)));
    }

    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(FluentExpander),
            new FrameworkPropertyMetadata(false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(FluentExpander),
            new PropertyMetadata(string.Empty));

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (GetTemplateChild("PART_ToggleButton") is Button toggleBtn)
            toggleBtn.Click += (_, _) => IsExpanded = !IsExpanded;
    }
}
