using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that smoothly transitions UI elements between
/// calm emphasis levels in response to user activity and focus changes.
/// <para>
/// <b>How it works:</b> Each element declares its <see cref="ZoneProperty"/>
/// (MenuBar, Toolbar, Content, StatusBar) and binds
/// <see cref="EmphasisProperty"/> to the emphasis level computed by
/// <c>ICalmUIService</c>. When the emphasis level changes, the behavior
/// animates a smooth 200 ms transition on the element's <c>Foreground</c>
/// color (for text softening) and a subtle <c>RenderTransform.ScaleX/Y</c>
/// reduction (for chrome shrinking).
/// </para>
///
/// <para><b>Activity detection:</b></para>
/// <para>
/// The <see cref="TrackActivityProperty"/> flag enables keyboard/mouse
/// activity monitoring on an element. When the user types or clicks
/// inside a zone, the behavior sets <see cref="IsUserActiveProperty"/>
/// to <c>True</c>. After an idle period (<see cref="IdleDelayMsProperty"/>,
/// default 800 ms), it resets to <c>False</c>. ViewModels can bind to
/// <c>IsUserActive</c> to trigger calm transitions on surrounding zones.
/// </para>
///
/// <para><b>Property channel allocation:</b></para>
/// <list type="table">
///   <listheader><term>Property</term><description>Owner</description></listheader>
///   <item><term><c>Opacity</c></term><description>BillingDimBehavior (do not touch)</description></item>
///   <item><term><c>BorderBrush</c></term><description>AdaptiveWorkspace (do not touch)</description></item>
///   <item><term><c>LayoutTransform</c></term><description>AdaptiveWorkspace (do not touch)</description></item>
///   <item><term><c>Effect</c></term><description>ActiveAreaHighlight (do not touch)</description></item>
///   <item><term><c>Background</c></term><description>ActiveAreaHighlight (do not touch)</description></item>
///   <item><term><c>RenderTransform.ScaleX/Y</c></term><description><b>CalmTransition</b> (this behavior)</description></item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;!-- Chrome zone: soften on calm --&gt;
/// &lt;Menu h:CalmTransition.Zone="MenuBar"
///       h:CalmTransition.Emphasis="{Binding CalmUI.CalmModeEnabled,
///           Converter={StaticResource ...}}" .../&gt;
///
/// &lt;!-- Activity tracking on workspace --&gt;
/// &lt;controls:ResponsiveContentControl
///       h:CalmTransition.TrackActivity="True"
///       h:CalmTransition.Zone="Content" .../&gt;
/// </code>
/// </summary>
public static class CalmTransition
{
    // ── Scale constants per emphasis level ────────────────────────

    /// <summary>Full emphasis — normal scale.</summary>
    private const double ScaleFull = 1.0;

    /// <summary>Muted emphasis — barely perceptible shrink.</summary>
    private const double ScaleMuted = 0.985;

    /// <summary>Receded emphasis — noticeable but not dramatic.</summary>
    private const double ScaleReceded = 0.97;

    /// <summary>Default idle timeout before the active state resets.</summary>
    private const int DefaultIdleDelayMs = 800;

    // ── Emphasis DP (master driver) ──────────────────────────────

    /// <summary>
    /// Bind to the current emphasis level for this element's zone.
    /// When this changes, smooth scale and foreground transitions play.
    /// Values: 0 = Full, 1 = Muted, 2 = Receded (matching <c>EmphasisLevel</c> enum).
    /// </summary>
    public static readonly DependencyProperty EmphasisProperty =
        DependencyProperty.RegisterAttached(
            "Emphasis",
            typeof(int),
            typeof(CalmTransition),
            new PropertyMetadata(0, OnEmphasisChanged));

    public static int GetEmphasis(DependencyObject obj) =>
        (int)obj.GetValue(EmphasisProperty);

    public static void SetEmphasis(DependencyObject obj, int value) =>
        obj.SetValue(EmphasisProperty, value);

    // ── Zone DP ──────────────────────────────────────────────────

    /// <summary>
    /// Identifies which <c>WorkspaceZone</c> this element belongs to.
    /// Used for logging/debugging — the emphasis level is the actual driver.
    /// </summary>
    public static readonly DependencyProperty ZoneProperty =
        DependencyProperty.RegisterAttached(
            "Zone",
            typeof(string),
            typeof(CalmTransition),
            new PropertyMetadata(string.Empty));

    public static string GetZone(DependencyObject obj) =>
        (string)obj.GetValue(ZoneProperty);

    public static void SetZone(DependencyObject obj, string value) =>
        obj.SetValue(ZoneProperty, value);

    // ── TrackActivity DP ─────────────────────────────────────────

    /// <summary>
    /// When <c>True</c>, monitors keyboard and mouse input on the
    /// element to detect user activity. Sets <see cref="IsUserActiveProperty"/>
    /// to <c>True</c> on input, resets after idle timeout.
    /// </summary>
    public static readonly DependencyProperty TrackActivityProperty =
        DependencyProperty.RegisterAttached(
            "TrackActivity",
            typeof(bool),
            typeof(CalmTransition),
            new PropertyMetadata(false, OnTrackActivityChanged));

    public static bool GetTrackActivity(DependencyObject obj) =>
        (bool)obj.GetValue(TrackActivityProperty);

    public static void SetTrackActivity(DependencyObject obj, bool value) =>
        obj.SetValue(TrackActivityProperty, value);

    // ── IsUserActive DP (read-only output) ───────────────────────

    /// <summary>
    /// <c>True</c> when the user is actively interacting with this
    /// element (typing, clicking). Resets to <c>False</c> after the
    /// idle delay. Bind surrounding zones to this to trigger calm.
    /// </summary>
    private static readonly DependencyPropertyKey IsUserActivePropertyKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "IsUserActive",
            typeof(bool),
            typeof(CalmTransition),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsUserActiveProperty =
        IsUserActivePropertyKey.DependencyProperty;

    public static bool GetIsUserActive(DependencyObject obj) =>
        (bool)obj.GetValue(IsUserActiveProperty);

    private static void SetIsUserActive(DependencyObject obj, bool value) =>
        obj.SetValue(IsUserActivePropertyKey, value);

    // ── IdleDelayMs DP ───────────────────────────────────────────

    /// <summary>
    /// Milliseconds of inactivity before <see cref="IsUserActiveProperty"/>
    /// resets to <c>False</c>. Default 800 ms.
    /// </summary>
    public static readonly DependencyProperty IdleDelayMsProperty =
        DependencyProperty.RegisterAttached(
            "IdleDelayMs",
            typeof(int),
            typeof(CalmTransition),
            new PropertyMetadata(DefaultIdleDelayMs));

    public static int GetIdleDelayMs(DependencyObject obj) =>
        (int)obj.GetValue(IdleDelayMsProperty);

    public static void SetIdleDelayMs(DependencyObject obj, int value) =>
        obj.SetValue(IdleDelayMsProperty, value);

    // ── Emphasis change handler ──────────────────────────────────

    private static void OnEmphasisChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe)
            return;

        var level = (int)e.NewValue;
        var duration = ResolveDuration(fe, "FluentDurationSlow", 200);
        var ease = ResolveEase(fe);

        AnimateScale(fe, level, duration, ease);
    }

    // ── Activity tracking wiring ─────────────────────────────────

    private static void OnTrackActivityChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement el)
            return;

        if (e.NewValue is true)
        {
            el.PreviewKeyDown += OnActivityDetected;
            el.PreviewMouseDown += OnActivityDetected;
            el.GotKeyboardFocus += OnFocusActivity;
        }
        else
        {
            el.PreviewKeyDown -= OnActivityDetected;
            el.PreviewMouseDown -= OnActivityDetected;
            el.GotKeyboardFocus -= OnFocusActivity;
        }
    }

    // ── Activity detection ───────────────────────────────────────

    // We use a ConditionalWeakTable to store per-element idle timers
    // without preventing GC of the elements.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<UIElement, System.Windows.Threading.DispatcherTimer>
        _idleTimers = new();

    private static void OnActivityDetected(object sender, EventArgs e)
    {
        if (sender is not UIElement el)
            return;

        MarkActive(el);
    }

    private static void OnFocusActivity(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not UIElement el)
            return;

        MarkActive(el);
    }

    private static void MarkActive(UIElement el)
    {
        SetIsUserActive(el, true);
        ResetIdleTimer(el);
    }

    private static void ResetIdleTimer(UIElement el)
    {
        if (!_idleTimers.TryGetValue(el, out var timer))
        {
            var delayMs = el is DependencyObject d
                ? GetIdleDelayMs(d)
                : DefaultIdleDelayMs;

            timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(delayMs)
            };

            timer.Tick += (_, _) =>
            {
                timer.Stop();
                SetIsUserActive(el, false);
            };

            _idleTimers.AddOrUpdate(el, timer);
        }

        timer.Stop();
        timer.Start();
    }

    // ── Scale animation ──────────────────────────────────────────

    private static void AnimateScale(
        FrameworkElement fe, int emphasisLevel, Duration duration, IEasingFunction? ease)
    {
        var targetScale = emphasisLevel switch
        {
            2 => ScaleReceded,
            1 => ScaleMuted,
            _ => ScaleFull
        };

        // Use RenderTransform so we don't conflict with
        // AdaptiveWorkspace's LayoutTransform
        if (fe.RenderTransform is not ScaleTransform st)
        {
            st = new ScaleTransform(1, 1);
            fe.RenderTransform = st;
            fe.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        var animX = new DoubleAnimation(targetScale, duration) { EasingFunction = ease };
        var animY = new DoubleAnimation(targetScale, duration) { EasingFunction = ease };
        animX.Freeze();
        animY.Freeze();

        st.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
        st.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
    }

    // ── Token resolution ─────────────────────────────────────────

    private static Duration ResolveDuration(FrameworkElement fe, string key, double fallbackMs)
    {
        var baseDuration = fe.TryFindResource(key) is Duration d && d.HasTimeSpan
            ? d.TimeSpan
            : TimeSpan.FromMilliseconds(fallbackMs);

        if (baseDuration == TimeSpan.Zero)
            return new Duration(TimeSpan.Zero);

        var flowState = Models.FlowState.Calm;
        var adapted = FlowMotionAdapter.GetAdaptedDuration(flowState, baseDuration);
        return new Duration(adapted);
    }

    private static IEasingFunction? ResolveEase(FrameworkElement fe) =>
        fe.TryFindResource("PanelDimEase") as IEasingFunction;
}
