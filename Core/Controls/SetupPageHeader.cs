using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Reusable page header for setup/configuration pages.
/// Renders a bold title + subtitle description with standard spacing.
/// </summary>
public class SetupPageHeader : Control
{
    static SetupPageHeader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SetupPageHeader), new FrameworkPropertyMetadata(typeof(SetupPageHeader)));
        FocusableProperty.OverrideMetadata(
            typeof(SetupPageHeader), new FrameworkPropertyMetadata(false));
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SetupPageHeader),
            new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(SetupPageHeader),
            new PropertyMetadata(string.Empty));

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
}
