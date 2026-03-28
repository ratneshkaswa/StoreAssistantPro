using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Windows 11 WinUI-style Expander with immediate expand/collapse.
/// Displays a header with a chevron indicator that toggles a content area.
/// </summary>
public class FluentExpander : HeaderedContentControl
{
    private Button? _toggleButton;
    private Border? _contentArea;

    static FluentExpander()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(FluentExpander), new FrameworkPropertyMetadata(typeof(FluentExpander)));
    }

    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(FluentExpander),
            new FrameworkPropertyMetadata(false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsExpandedChanged));

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

    protected override AutomationPeer OnCreateAutomationPeer() => new FluentExpanderAutomationPeer(this);

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_toggleButton is not null)
            _toggleButton.Click -= OnToggleButtonClick;

        _toggleButton = GetTemplateChild("PART_ToggleButton") as Button;
        _contentArea = GetTemplateChild("PART_ContentArea") as Border;

        if (_toggleButton is not null)
            _toggleButton.Click += OnToggleButtonClick;

        ApplyExpandedState();
    }

    private void OnToggleButtonClick(object sender, RoutedEventArgs e) => IsExpanded = !IsExpanded;

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FluentExpander expander)
            expander.UpdateExpandedState();
    }

    private void UpdateExpandedState()
    {
        if (_contentArea is null)
            return;

        ApplyExpandedState();
    }

    private void ApplyExpandedState()
    {
        if (_contentArea is null)
            return;

        if (IsExpanded)
        {
            _contentArea.Visibility = Visibility.Visible;
            _contentArea.Height = double.NaN;
            _contentArea.Opacity = 1;
        }
        else
        {
            _contentArea.Visibility = Visibility.Collapsed;
            _contentArea.Height = 0;
            _contentArea.Opacity = 0;
        }
    }

    private sealed class FluentExpanderAutomationPeer(FluentExpander owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(FluentExpander);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override string GetNameCore()
        {
            var explicitName = base.GetNameCore();
            var owner = (FluentExpander)Owner;
            var headerText = owner.Header?.ToString();

            if (!string.IsNullOrWhiteSpace(explicitName) &&
                !string.Equals(explicitName, headerText, StringComparison.Ordinal))
            {
                return explicitName;
            }

            if (string.IsNullOrWhiteSpace(headerText))
                headerText = "Section";

            return $"{headerText}, {(owner.IsExpanded ? "expanded" : "collapsed")}";
        }
    }
}
