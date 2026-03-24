using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Win11-style split button: primary action on the main segment, flyout menu on the chevron segment.
/// </summary>
public class SplitButton : ContentControl
{
    private ButtonBase? _dropDownButton;

    static SplitButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SplitButton),
            new FrameworkPropertyMetadata(typeof(SplitButton)));
    }

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(SplitButton));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(SplitButton));

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_dropDownButton is not null)
        {
            _dropDownButton.Click -= OnDropDownButtonClick;
        }

        _dropDownButton = GetTemplateChild("PART_DropDownButton") as ButtonBase;
        if (_dropDownButton is not null)
        {
            _dropDownButton.Click += OnDropDownButtonClick;
        }
    }

    private void OnDropDownButtonClick(object sender, RoutedEventArgs e)
    {
        if (ContextMenu is not { Items.Count: > 0 } menu || _dropDownButton is null)
        {
            return;
        }

        menu.DataContext = DataContext;
        menu.PlacementTarget = _dropDownButton;
        menu.Placement = PlacementMode.Bottom;
        menu.MinWidth = ActualWidth;
        menu.IsOpen = true;
    }
}
