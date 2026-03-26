using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Win11-style split button: primary action on the main segment, flyout menu on the chevron segment.
/// </summary>
public class SplitButton : ContentControl
{
    private ButtonBase? _primaryButton;
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

    protected override AutomationPeer OnCreateAutomationPeer() => new SplitButtonAutomationPeer(this);

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_dropDownButton is not null)
        {
            _dropDownButton.Click -= OnDropDownButtonClick;
        }

        _primaryButton = GetTemplateChild("PART_PrimaryButton") as ButtonBase;
        if (_primaryButton is not null)
            AutomationProperties.SetName(_primaryButton, GetPrimaryAutomationName());

        _dropDownButton = GetTemplateChild("PART_DropDownButton") as ButtonBase;
        if (_dropDownButton is not null)
        {
            _dropDownButton.Click += OnDropDownButtonClick;
        }
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);

        if (_primaryButton is not null)
            AutomationProperties.SetName(_primaryButton, GetPrimaryAutomationName());
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

    private string GetPrimaryAutomationName()
    {
        var label = Content?.ToString();
        return string.IsNullOrWhiteSpace(label)
            ? "Primary action"
            : label.Trim();
    }

    private sealed class SplitButtonAutomationPeer(SplitButton owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(SplitButton);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.SplitButton;

        protected override string GetNameCore()
        {
            var explicitName = base.GetNameCore();
            if (!string.IsNullOrWhiteSpace(explicitName))
                return explicitName;

            var owner = (SplitButton)Owner;
            var label = owner.Content?.ToString();
            return string.IsNullOrWhiteSpace(label)
                ? "Split action"
                : label.Trim();
        }
    }
}
