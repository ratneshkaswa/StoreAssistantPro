using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Windows 11 WinUI-style Expander with animated expand/collapse.
/// Displays a header with a chevron indicator that toggles a content area.
/// </summary>
public class FluentExpander : HeaderedContentControl
{
    private const int ExpandDurationMs = 120;
    private const int CollapseDurationMs = 100;

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
            expander.UpdateExpandedState(useTransitions: true);
    }

    private void UpdateExpandedState(bool useTransitions)
    {
        if (_contentArea is null)
            return;

        if (!useTransitions || !IsLoaded)
        {
            ApplyExpandedState();
            return;
        }

        if (IsExpanded)
            AnimateExpand();
        else
            AnimateCollapse();
    }

    private void ApplyExpandedState()
    {
        if (_contentArea is null)
            return;

        StopContentAnimations();

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

    private void AnimateExpand()
    {
        if (_contentArea is null)
            return;

        StopContentAnimations();

        _contentArea.Visibility = Visibility.Visible;
        var targetHeight = MeasureExpandedHeight();
        _contentArea.Height = 0;
        _contentArea.Opacity = 0;

        var heightAnimation = new DoubleAnimation(0, targetHeight, TimeSpan.FromMilliseconds(ExpandDurationMs))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        heightAnimation.Completed += (_, _) =>
        {
            if (!IsExpanded || _contentArea is null)
                return;

            StopContentAnimations();
            _contentArea.Height = double.NaN;
            _contentArea.Opacity = 1;
        };

        _contentArea.BeginAnimation(HeightProperty, heightAnimation, HandoffBehavior.SnapshotAndReplace);
        _contentArea.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(ExpandDurationMs))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            },
            HandoffBehavior.SnapshotAndReplace);
    }

    private void AnimateCollapse()
    {
        if (_contentArea is null)
            return;

        StopContentAnimations();

        var startHeight = _contentArea.ActualHeight;
        if (startHeight <= 0)
            startHeight = MeasureExpandedHeight();

        if (startHeight <= 0)
        {
            ApplyExpandedState();
            return;
        }

        _contentArea.Visibility = Visibility.Visible;
        _contentArea.Height = startHeight;
        _contentArea.Opacity = 1;

        var heightAnimation = new DoubleAnimation(startHeight, 0, TimeSpan.FromMilliseconds(CollapseDurationMs))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        heightAnimation.Completed += (_, _) =>
        {
            if (IsExpanded || _contentArea is null)
                return;

            StopContentAnimations();
            _contentArea.Visibility = Visibility.Collapsed;
            _contentArea.Height = 0;
            _contentArea.Opacity = 0;
        };

        _contentArea.BeginAnimation(HeightProperty, heightAnimation, HandoffBehavior.SnapshotAndReplace);
        _contentArea.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(CollapseDurationMs))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            },
            HandoffBehavior.SnapshotAndReplace);
    }

    private void StopContentAnimations()
    {
        if (_contentArea is null)
            return;

        _contentArea.BeginAnimation(HeightProperty, null);
        _contentArea.BeginAnimation(OpacityProperty, null);
    }

    private double MeasureExpandedHeight()
    {
        if (_contentArea is null)
            return 0;

        var availableWidth = _contentArea.ActualWidth;
        if (availableWidth <= 0)
            availableWidth = ActualWidth;

        if (availableWidth <= 0)
            availableWidth = double.PositiveInfinity;

        _contentArea.Measure(new Size(availableWidth, double.PositiveInfinity));
        return Math.Max(0, _contentArea.DesiredSize.Height);
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
