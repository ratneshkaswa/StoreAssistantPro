using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Compact inline banner that displays a guidance tip inside a window.
/// </summary>
public class InlineTipBanner : Control
{
    public static readonly RoutedEvent DismissedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(Dismissed),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(InlineTipBanner));

    public event RoutedEventHandler Dismissed
    {
        add => AddHandler(DismissedEvent, value);
        remove => RemoveHandler(DismissedEvent, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(InlineTipBanner),
            new PropertyMetadata("Tip"));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TipTextProperty =
        DependencyProperty.Register(
            nameof(TipText), typeof(string), typeof(InlineTipBanner),
            new PropertyMetadata(string.Empty));

    public string TipText
    {
        get => (string)GetValue(TipTextProperty);
        set => SetValue(TipTextProperty, value);
    }

    public static readonly DependencyProperty IsDismissedProperty =
        DependencyProperty.Register(
            nameof(IsDismissed), typeof(bool), typeof(InlineTipBanner),
            new FrameworkPropertyMetadata(false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsDismissedChanged));

    public bool IsDismissed
    {
        get => (bool)GetValue(IsDismissedProperty);
        set => SetValue(IsDismissedProperty, value);
    }

    public static readonly DependencyProperty DismissCommandProperty =
        DependencyProperty.Register(
            nameof(DismissCommand), typeof(ICommand), typeof(InlineTipBanner));

    public ICommand? DismissCommand
    {
        get => (ICommand?)GetValue(DismissCommandProperty);
        set => SetValue(DismissCommandProperty, value);
    }

    private const string PartRoot = "PART_Root";
    private const string PartCloseButton = "PART_CloseButton";

    private Border? _root;
    private Button? _closeButton;
    private bool _isDismissing;

    static InlineTipBanner()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(InlineTipBanner),
            new FrameworkPropertyMetadata(typeof(InlineTipBanner)));
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new InlineTipBannerAutomationPeer(this);

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_closeButton is not null)
        {
            _closeButton.Click -= OnCloseButtonClick;
            _closeButton = null;
        }

        _root = GetTemplateChild(PartRoot) as Border;
        _closeButton = GetTemplateChild(PartCloseButton) as Button;

        if (_closeButton is not null)
            _closeButton.Click += OnCloseButtonClick;

        if (IsDismissed)
        {
            Visibility = Visibility.Collapsed;
            if (_root is not null)
                _root.Opacity = 0;
        }
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e) => DismissImmediately();

    private void DismissImmediately()
    {
        if (_isDismissing)
            return;

        _isDismissing = true;
        if (_root is not null)
        {
            _root.BeginAnimation(OpacityProperty, null);
            _root.BeginAnimation(HeightProperty, null);
            _root.Opacity = 1;
            _root.Height = double.NaN;
        }

        FinaliseDismiss();
    }

    private void FinaliseDismiss()
    {
        Visibility = Visibility.Collapsed;
        _isDismissing = true;
        IsDismissed = true;
        _isDismissing = false;

        if (DismissCommand is { } cmd && cmd.CanExecute(null))
            cmd.Execute(null);

        RaiseEvent(new RoutedEventArgs(DismissedEvent, this));
    }

    private static void OnIsDismissedChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not InlineTipBanner banner)
            return;

        if (e.NewValue is true)
        {
            if (!banner._isDismissing)
                banner.DismissImmediately();
        }
        else
        {
            banner._isDismissing = false;

            if (banner._root is not null)
            {
                banner._root.BeginAnimation(OpacityProperty, null);
                banner._root.BeginAnimation(HeightProperty, null);
                banner._root.Opacity = 1;
                banner._root.Height = double.NaN;
            }

            banner.Visibility = Visibility.Visible;
        }
    }

    private sealed class InlineTipBannerAutomationPeer(InlineTipBanner owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(InlineTipBanner);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override string GetNameCore()
        {
            var explicitName = base.GetNameCore();
            if (!string.IsNullOrWhiteSpace(explicitName))
                return explicitName;

            var owner = (InlineTipBanner)Owner;
            return string.IsNullOrWhiteSpace(owner.TipText)
                ? owner.Title
                : $"{owner.Title}: {owner.TipText}";
        }
    }
}
