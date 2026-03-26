using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Overlay control that renders toast notifications in the bottom-right
/// corner of the window. Bind <see cref="Toasts"/> to the
/// <see cref="Services.IToastService.Toasts"/> collection.
/// </summary>
public class ToastHost : Control
{
    public static readonly DependencyProperty ToastsProperty =
        DependencyProperty.Register(
            nameof(Toasts), typeof(ObservableCollection<Services.ToastItem>),
            typeof(ToastHost),
            new PropertyMetadata(null));

    public ObservableCollection<Services.ToastItem>? Toasts
    {
        get => (ObservableCollection<Services.ToastItem>?)GetValue(ToastsProperty);
        set => SetValue(ToastsProperty, value);
    }

    static ToastHost()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ToastHost),
            new FrameworkPropertyMetadata(typeof(ToastHost)));

        FocusableProperty.OverrideMetadata(
            typeof(ToastHost),
            new FrameworkPropertyMetadata(false));
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new ToastHostAutomationPeer(this);

    private sealed class ToastHostAutomationPeer(ToastHost owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(ToastHost);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

        protected override string GetNameCore()
        {
            var explicitName = base.GetNameCore();
            return !string.IsNullOrWhiteSpace(explicitName)
                ? explicitName
                : "Notifications";
        }
    }
}
