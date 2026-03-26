using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Markup;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Reusable card for setup/configuration pages.
/// Provides the standard flat card border + two-column form grid.
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

    public static readonly DependencyProperty FormContentProperty =
        DependencyProperty.Register(nameof(FormContent), typeof(object), typeof(SetupFormCard),
            new PropertyMetadata(null));

    public object? FormContent
    {
        get => GetValue(FormContentProperty);
        set => SetValue(FormContentProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new SetupFormCardAutomationPeer(this);

    private sealed class SetupFormCardAutomationPeer(SetupFormCard owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(SetupFormCard);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override string GetNameCore()
        {
            var explicitName = base.GetNameCore();
            return string.IsNullOrWhiteSpace(explicitName) ? "Setup form" : explicitName;
        }
    }
}
