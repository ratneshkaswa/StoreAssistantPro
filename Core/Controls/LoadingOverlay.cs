using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Threading;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Semi-transparent overlay with a compact progress surface and supporting
/// <see cref="ProgressRing"/> that covers its parent when
/// <see cref="IsActive"/> is <c>true</c>.
/// <para>
/// Place as the last child inside a <see cref="Grid"/> so it layers on top.
/// </para>
/// <example>
/// <code>
/// &lt;Grid&gt;
///     &lt;!-- Normal content --&gt;
///     &lt;controls:LoadingOverlay IsActive="{Binding IsLoading}"/&gt;
/// &lt;/Grid&gt;
/// </code>
/// </example>
/// </summary>
public class LoadingOverlay : Control
{
    private const int DefaultShowDelayMs = 120;
    private const int DefaultMinimumVisibleMs = 180;
    private DispatcherTimer? _showTimer;
    private DispatcherTimer? _hideTimer;
    private DateTime _shownAtUtc;

    static LoadingOverlay()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(LoadingOverlay), new FrameworkPropertyMetadata(typeof(LoadingOverlay)));
    }

    public LoadingOverlay()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(LoadingOverlay),
            new PropertyMetadata(false, OnIsActiveChanged));

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    private static readonly DependencyPropertyKey IsDisplayActivePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsDisplayActive),
            typeof(bool),
            typeof(LoadingOverlay),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsDisplayActiveProperty =
        IsDisplayActivePropertyKey.DependencyProperty;

    public bool IsDisplayActive
    {
        get => (bool)GetValue(IsDisplayActiveProperty);
        private set => SetValue(IsDisplayActivePropertyKey, value);
    }

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(LoadingOverlay),
            new PropertyMetadata("Preparing content"));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public static readonly DependencyProperty ShowDelayProperty =
        DependencyProperty.Register(nameof(ShowDelay), typeof(TimeSpan), typeof(LoadingOverlay),
            new PropertyMetadata(TimeSpan.FromMilliseconds(DefaultShowDelayMs)));

    public TimeSpan ShowDelay
    {
        get => (TimeSpan)GetValue(ShowDelayProperty);
        set => SetValue(ShowDelayProperty, value);
    }

    public static readonly DependencyProperty MinimumVisibleDurationProperty =
        DependencyProperty.Register(nameof(MinimumVisibleDuration), typeof(TimeSpan), typeof(LoadingOverlay),
            new PropertyMetadata(TimeSpan.FromMilliseconds(DefaultMinimumVisibleMs)));

    public TimeSpan MinimumVisibleDuration
    {
        get => (TimeSpan)GetValue(MinimumVisibleDurationProperty);
        set => SetValue(MinimumVisibleDurationProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new LoadingOverlayAutomationPeer(this);

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LoadingOverlay overlay)
            overlay.SyncDisplayState();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => SyncDisplayState();

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _showTimer?.Stop();
        _hideTimer?.Stop();
        IsDisplayActive = false;
    }

    private void SyncDisplayState()
    {
        _hideTimer?.Stop();

        if (IsActive)
        {
            if (IsDisplayActive)
                return;

            var delay = ShowDelay;
            if (delay <= TimeSpan.Zero)
            {
                ShowNow();
                return;
            }

            _showTimer ??= CreateTimer(OnShowTimerTick);
            _showTimer.Interval = delay;
            _showTimer.Stop();
            _showTimer.Start();
            return;
        }

        _showTimer?.Stop();
        if (!IsDisplayActive)
            return;

        var remaining = MinimumVisibleDuration - (DateTime.UtcNow - _shownAtUtc);
        if (remaining <= TimeSpan.Zero)
        {
            HideNow();
            return;
        }

        _hideTimer ??= CreateTimer(OnHideTimerTick);
        _hideTimer.Interval = remaining;
        _hideTimer.Stop();
        _hideTimer.Start();
    }

    private DispatcherTimer CreateTimer(EventHandler tickHandler)
    {
        var timer = Dispatcher is not null
            ? new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            : new DispatcherTimer();

        timer.Tick += tickHandler;
        return timer;
    }

    private void OnShowTimerTick(object? sender, EventArgs e)
    {
        _showTimer?.Stop();
        if (IsActive)
            ShowNow();
    }

    private void OnHideTimerTick(object? sender, EventArgs e)
    {
        _hideTimer?.Stop();
        if (!IsActive)
            HideNow();
    }

    private void ShowNow()
    {
        _hideTimer?.Stop();
        _shownAtUtc = DateTime.UtcNow;
        IsDisplayActive = true;
    }

    private void HideNow()
    {
        _showTimer?.Stop();
        IsDisplayActive = false;
    }

    private sealed class LoadingOverlayAutomationPeer(LoadingOverlay owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(LoadingOverlay);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;

        protected override string GetNameCore()
        {
            var explicitName = base.GetNameCore();
            if (!string.IsNullOrWhiteSpace(explicitName))
                return explicitName;

            var owner = (LoadingOverlay)Owner;
            return string.IsNullOrWhiteSpace(owner.Message) ? "Loading content" : owner.Message;
        }
    }
}
