using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Windows 11 WinUI-style InfoBar for inline status messages.
/// Displays a coloured strip with icon + message that auto-maps to
/// severity levels (Info, Success, Warning, Error).
/// </summary>
public class InfoBar : ContentControl
{
    private Button? _closeButton;

    static InfoBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(InfoBar), new FrameworkPropertyMetadata(typeof(InfoBar)));
    }

    public static readonly DependencyProperty SeverityProperty =
        DependencyProperty.Register(nameof(Severity), typeof(InfoBarSeverity), typeof(InfoBar),
            new PropertyMetadata(InfoBarSeverity.Info));

    public InfoBarSeverity Severity
    {
        get => (InfoBarSeverity)GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(InfoBar),
            new PropertyMetadata(string.Empty));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(InfoBar),
            new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty IsClosableProperty =
        DependencyProperty.Register(nameof(IsClosable), typeof(bool), typeof(InfoBar),
            new PropertyMetadata(false));

    public bool IsClosable
    {
        get => (bool)GetValue(IsClosableProperty);
        set => SetValue(IsClosableProperty, value);
    }

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(InfoBar),
            new PropertyMetadata(true));

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new InfoBarAutomationPeer(this);

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_closeButton is not null)
            _closeButton.Click -= OnCloseButtonClick;

        _closeButton = GetTemplateChild("PART_CloseButton") as Button;

        if (_closeButton is not null)
            _closeButton.Click += OnCloseButtonClick;
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e) => IsOpen = false;

    private sealed class InfoBarAutomationPeer(InfoBar owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(InfoBar);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.StatusBar;

        protected override string GetNameCore()
        {
            var explicitName = base.GetNameCore();
            if (!string.IsNullOrWhiteSpace(explicitName))
                return explicitName;

            var owner = (InfoBar)Owner;
            return string.IsNullOrWhiteSpace(owner.Title)
                ? owner.Message ?? "Status message"
                : $"{owner.Title}: {owner.Message}".Trim();
        }
    }
}

public enum InfoBarSeverity
{
    Info,
    Success,
    Warning,
    Error
}
