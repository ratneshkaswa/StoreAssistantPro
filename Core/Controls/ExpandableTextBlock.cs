using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Collapsible multi-line text surface with a small Show more/Show less toggle.
/// </summary>
public class ExpandableTextBlock : Control
{
    private const int ToggleDurationMs = 180;

    private Border? _contentHost;
    private TextBlock? _textBlock;
    private Button? _toggleButton;
    private bool _isDeferredUpdateQueued;

    static ExpandableTextBlock()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ExpandableTextBlock), new FrameworkPropertyMetadata(typeof(ExpandableTextBlock)));
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(ExpandableTextBlock),
            new PropertyMetadata(string.Empty, OnLayoutPropertyChanged));

    public static readonly DependencyProperty CollapsedLineCountProperty =
        DependencyProperty.Register(
            nameof(CollapsedLineCount),
            typeof(int),
            typeof(ExpandableTextBlock),
            new PropertyMetadata(3, OnLayoutPropertyChanged));

    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(
            nameof(IsExpanded),
            typeof(bool),
            typeof(ExpandableTextBlock),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsExpandedChanged));

    public static readonly DependencyProperty ShowMoreTextProperty =
        DependencyProperty.Register(
            nameof(ShowMoreText),
            typeof(string),
            typeof(ExpandableTextBlock),
            new PropertyMetadata("Show more", OnLayoutPropertyChanged));

    public static readonly DependencyProperty ShowLessTextProperty =
        DependencyProperty.Register(
            nameof(ShowLessText),
            typeof(string),
            typeof(ExpandableTextBlock),
            new PropertyMetadata("Show less", OnLayoutPropertyChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public int CollapsedLineCount
    {
        get => (int)GetValue(CollapsedLineCountProperty);
        set => SetValue(CollapsedLineCountProperty, value);
    }

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public string ShowMoreText
    {
        get => (string)GetValue(ShowMoreTextProperty);
        set => SetValue(ShowMoreTextProperty, value);
    }

    public string ShowLessText
    {
        get => (string)GetValue(ShowLessTextProperty);
        set => SetValue(ShowLessTextProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_toggleButton is not null)
        {
            _toggleButton.Click -= OnToggleButtonClick;
        }

        _contentHost = GetTemplateChild("PART_ContentHost") as Border;
        _textBlock = GetTemplateChild("PART_TextBlock") as TextBlock;
        _toggleButton = GetTemplateChild("PART_ToggleButton") as Button;

        if (_toggleButton is not null)
        {
            _toggleButton.Click += OnToggleButtonClick;
        }

        ScheduleVisualStateRefresh();
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);

        if (sizeInfo.WidthChanged)
        {
            ScheduleVisualStateRefresh();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => ScheduleVisualStateRefresh();

    private void OnUnloaded(object sender, RoutedEventArgs e) => StopAnimations();

    private void OnToggleButtonClick(object sender, RoutedEventArgs e) => IsExpanded = !IsExpanded;

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ExpandableTextBlock control)
        {
            control.ScheduleVisualStateRefresh();
        }
    }

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ExpandableTextBlock control)
        {
            control.UpdateVisualState(useTransitions: true);
        }
    }

    private void ScheduleVisualStateRefresh()
    {
        if (_isDeferredUpdateQueued || !Dispatcher.CheckAccess())
        {
            if (!_isDeferredUpdateQueued)
            {
                _isDeferredUpdateQueued = true;
                Dispatcher.BeginInvoke(
                    DispatcherPriority.Loaded,
                    new Action(() =>
                    {
                        _isDeferredUpdateQueued = false;
                        UpdateVisualState(useTransitions: false);
                    }));
            }

            return;
        }

        _isDeferredUpdateQueued = true;
        Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() =>
            {
                _isDeferredUpdateQueued = false;
                UpdateVisualState(useTransitions: false);
            }));
    }

    private void UpdateVisualState(bool useTransitions)
    {
        if (_contentHost is null || _textBlock is null || _toggleButton is null)
        {
            return;
        }

        var text = Text ?? string.Empty;
        var availableWidth = GetAvailableTextWidth();
        if (availableWidth <= 0)
        {
            ApplyUnboundedState();
            return;
        }

        var fullHeight = MeasureTextHeight(text, availableWidth);
        var collapsedHeight = Math.Min(fullHeight, MeasureCollapsedHeight(availableWidth));
        var canExpand = !string.IsNullOrWhiteSpace(text) && fullHeight > collapsedHeight + 1;

        _toggleButton.Visibility = canExpand ? Visibility.Visible : Visibility.Collapsed;
        _toggleButton.Content = IsExpanded ? ShowLessText : ShowMoreText;
        AutomationProperties.SetName(_toggleButton, _toggleButton.Content?.ToString() ?? "Toggle text");
        _toggleButton.ToolTip = _toggleButton.Content;

        if (!canExpand)
        {
            ApplyUnboundedState();
            return;
        }

        if (!useTransitions || !IsLoaded)
        {
            ApplyMeasuredState(fullHeight, collapsedHeight);
            return;
        }

        AnimateMeasuredState(fullHeight, collapsedHeight);
    }

    private void ApplyUnboundedState()
    {
        if (_contentHost is null || _textBlock is null)
        {
            return;
        }

        StopAnimations();
        _textBlock.TextTrimming = TextTrimming.None;
        _contentHost.Height = double.NaN;
    }

    private void ApplyMeasuredState(double fullHeight, double collapsedHeight)
    {
        if (_contentHost is null || _textBlock is null)
        {
            return;
        }

        StopAnimations();

        if (IsExpanded)
        {
            _textBlock.TextTrimming = TextTrimming.None;
            _contentHost.Height = double.NaN;
            return;
        }

        _textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
        _contentHost.Height = collapsedHeight;
    }

    private void AnimateMeasuredState(double fullHeight, double collapsedHeight)
    {
        if (_contentHost is null || _textBlock is null)
        {
            return;
        }

        StopAnimations();

        var targetHeight = IsExpanded ? fullHeight : collapsedHeight;
        var startingHeight = _contentHost.ActualHeight;

        if (startingHeight <= 0 || double.IsNaN(startingHeight))
        {
            startingHeight = IsExpanded ? collapsedHeight : fullHeight;
        }

        if (Math.Abs(startingHeight - targetHeight) < 0.5)
        {
            ApplyMeasuredState(fullHeight, collapsedHeight);
            return;
        }

        _textBlock.TextTrimming = TextTrimming.None;
        _contentHost.Height = startingHeight;

        var animation = new DoubleAnimation(startingHeight, targetHeight, TimeSpan.FromMilliseconds(ToggleDurationMs))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        animation.Completed += (_, _) =>
        {
            if (_contentHost is null || _textBlock is null)
            {
                return;
            }

            StopAnimations();
            ApplyMeasuredState(fullHeight, collapsedHeight);
        };

        _contentHost.BeginAnimation(HeightProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }

    private void StopAnimations()
    {
        _contentHost?.BeginAnimation(HeightProperty, null);
    }

    private double GetAvailableTextWidth()
    {
        if (_contentHost is null || _textBlock is null)
        {
            return 0;
        }

        var width = _contentHost.ActualWidth;
        if (width <= 0)
        {
            width = ActualWidth;
        }

        if (width <= 0)
        {
            return 0;
        }

        var margin = _textBlock.Margin;
        return Math.Max(0, width - margin.Left - margin.Right);
    }

    private double MeasureCollapsedHeight(double availableWidth)
    {
        var collapsedLines = Math.Max(1, CollapsedLineCount);
        var lineHeight = MeasureSingleLineHeight(availableWidth);
        return Math.Ceiling(lineHeight * collapsedLines);
    }

    private double MeasureSingleLineHeight(double availableWidth)
    {
        var probe = CreateProbeTextBlock("Ag");
        probe.TextWrapping = TextWrapping.NoWrap;
        probe.Measure(new Size(Math.Max(availableWidth, 1), double.PositiveInfinity));
        return Math.Max(1, probe.DesiredSize.Height);
    }

    private double MeasureTextHeight(string text, double availableWidth)
    {
        var probe = CreateProbeTextBlock(text);
        probe.TextWrapping = TextWrapping.Wrap;
        probe.TextTrimming = TextTrimming.None;
        probe.Measure(new Size(Math.Max(availableWidth, 1), double.PositiveInfinity));
        return Math.Max(0, Math.Ceiling(probe.DesiredSize.Height));
    }

    private TextBlock CreateProbeTextBlock(string text) =>
        new()
        {
            Text = text,
            FontFamily = FontFamily,
            FontSize = FontSize,
            FontStyle = FontStyle,
            FontWeight = FontWeight,
            FontStretch = FontStretch
        };
}
