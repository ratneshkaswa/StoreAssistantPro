using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Reusable card for setup/configuration pages.
/// Provides the standard flat card border + two-column form Grid
/// (label column + input column). Place form row elements directly
/// inside as content — they become children of the inner Grid.
/// </summary>
[ContentProperty(nameof(FormContent))]
public class SetupFormCard : Control
{
    static SetupFormCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SetupFormCard), new FrameworkPropertyMetadata(typeof(SetupFormCard)));
        FocusableProperty.OverrideMetadata(
            typeof(SetupFormCard), new FrameworkPropertyMetadata(false));
    }

    /// <summary>
    /// The content placed inside the card. Typically a Grid with
    /// RowDefinitions and form elements using Grid.Row/Grid.Column.
    /// </summary>
    public static readonly DependencyProperty FormContentProperty =
        DependencyProperty.Register(nameof(FormContent), typeof(object), typeof(SetupFormCard),
            new PropertyMetadata(null));

    public object? FormContent
    {
        get => GetValue(FormContentProperty);
        set => SetValue(FormContentProperty, value);
    }
}
