using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that adds a subtle transition animation to
/// status badge pills when their state changes.
/// <para>
/// Designed for <c>StatusBadgePillStyle</c> borders in the status
/// bar and toolbar whose <c>Background</c> changes via
/// <c>DataTrigger</c> setters (mode, connectivity, focus lock).
/// </para>
/// <para>
/// <b>Animation:</b> When the <c>Background</c> brush changes, the
/// element plays a single-shot opacity dip (1 → 0.3 → 1) paired
/// with a gentle scale pulse (1 → 1.06 → 1). Together these give
/// a brief "flash" that draws the eye without sustained motion.
/// </para>
/// <para>
/// All timings use <c>DesignSystem.xaml</c> tokens
/// (<c>FluentDurationNormal</c>, <c>FluentEasePoint</c>).
/// Animations target <c>Opacity</c> and <c>RenderTransform</c>
/// only — zero layout cost, GPU-composited.
/// </para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;Border h:StatusPillTransition.AnimateChanges="True" ...&gt;
/// </code>
/// </summary>
public static class StatusPillTransition
{
    // ── AnimateChanges DP ────────────────────────────────────────────

    /// <summary>
    /// Set to <c>True</c> on any status pill border to enable smooth
    /// transition animations when its <c>Background</c> changes.
    /// </summary>
    public static readonly DependencyProperty AnimateChangesProperty =
        DependencyProperty.RegisterAttached(
            "AnimateChanges",
            typeof(bool),
            typeof(StatusPillTransition),
            new PropertyMetadata(false, OnAnimateChangesChanged));

    public static bool GetAnimateChanges(DependencyObject obj) =>
        (bool)obj.GetValue(AnimateChangesProperty);

    public static void SetAnimateChanges(DependencyObject obj, bool value) =>
        obj.SetValue(AnimateChangesProperty, value);

    // ── Private state ────────────────────────────────────────────────

    private static readonly DependencyProperty TrackerProperty =
        DependencyProperty.RegisterAttached(
            "Tracker",
            typeof(BackgroundTracker),
            typeof(StatusPillTransition));

    // ── Attach / detach ──────────────────────────────────────────────

    private static void OnAnimateChangesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe)
            return;

        if (e.NewValue is true)
        {
            fe.Loaded += OnElementLoaded;
            fe.Unloaded += OnElementUnloaded;

            // If already loaded, attach immediately
            if (fe.IsLoaded)
                AttachTracker(fe);
        }
        else
        {
            fe.Loaded -= OnElementLoaded;
            fe.Unloaded -= OnElementUnloaded;
            DetachTracker(fe);
        }
    }

    private static void OnElementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
            AttachTracker(fe);
    }

    private static void OnElementUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
            DetachTracker(fe);
    }

    private static void AttachTracker(FrameworkElement fe)
    {
        if (fe.GetValue(TrackerProperty) is not null)
            return; // Already attached

        var tracker = new BackgroundTracker(fe);
        fe.SetValue(TrackerProperty, tracker);
    }

    private static void DetachTracker(FrameworkElement fe)
    {
        var tracker = fe.GetValue(TrackerProperty) as BackgroundTracker;
        tracker?.Detach();
        fe.ClearValue(TrackerProperty);
    }

    // ── Tracker (listens for Background changes) ─────────────────────

    /// <summary>
    /// Watches the element's <c>Background</c> property via the WPF
    /// property descriptor notification system.
    /// </summary>
    private sealed class BackgroundTracker
    {
        private readonly FrameworkElement _target;
        private readonly DependencyPropertyDescriptor? _descriptor;
        private Brush? _lastBackground;
        private bool _initialized;

        public BackgroundTracker(FrameworkElement target)
        {
            _target = target;

            EnsureScaleTransform();

            _descriptor = DependencyPropertyDescriptor.FromProperty(
                System.Windows.Controls.Border.BackgroundProperty,
                typeof(System.Windows.Controls.Border));

            if (_descriptor is not null)
                _descriptor.AddValueChanged(target, OnBackgroundChanged);

            // Capture initial value so first DataTrigger evaluation
            // (which fires during layout) doesn't trigger a spurious
            // animation.
            if (target is System.Windows.Controls.Border border)
                _lastBackground = border.Background;
        }

        public void Detach()
        {
            if (_descriptor is not null)
                _descriptor.RemoveValueChanged(_target, OnBackgroundChanged);
        }

        private void OnBackgroundChanged(object? sender, EventArgs e)
        {
            if (_target is not System.Windows.Controls.Border border)
                return;

            var current = border.Background;

            // Skip the very first change (initial template application)
            if (!_initialized)
            {
                _initialized = true;
                _lastBackground = current;
                return;
            }

            // Only animate when the brush actually differs
            if (BrushesEqual(_lastBackground, current))
                return;

            _lastBackground = current;
            PlayPulse();
        }

        private void PlayPulse()
        {
            var duration = ResolveDuration();
            var ease = ResolveEase();

            var halfTime = TimeSpan.FromMilliseconds(duration.TotalMilliseconds / 2);

            // ── Opacity dip: 1 → 0.3 → 1 ────────────────────────────
            var opacityAnim = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
            opacityAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0.3, KeyTime.FromTimeSpan(halfTime), ease));
            opacityAnim.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(duration), ease));
            opacityAnim.Freeze();

            // ── Scale pulse: 1 → 1.06 → 1 ───────────────────────────
            var scaleUpX = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
            scaleUpX.KeyFrames.Add(new EasingDoubleKeyFrame(1.06, KeyTime.FromTimeSpan(halfTime), ease));
            scaleUpX.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(duration), ease));
            scaleUpX.Freeze();

            var scaleUpY = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
            scaleUpY.KeyFrames.Add(new EasingDoubleKeyFrame(1.06, KeyTime.FromTimeSpan(halfTime), ease));
            scaleUpY.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(duration), ease));
            scaleUpY.Freeze();

            _target.BeginAnimation(UIElement.OpacityProperty, opacityAnim);

            if (_target.RenderTransform is ScaleTransform st)
            {
                st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUpX);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUpY);
            }
        }

        // ── Transform setup ──────────────────────────────────────────

        private void EnsureScaleTransform()
        {
            _target.RenderTransformOrigin = new Point(0.5, 0.5);

            if (_target.RenderTransform is ScaleTransform)
                return;

            if (_target.RenderTransform is null or MatrixTransform)
            {
                _target.RenderTransform = new ScaleTransform(1, 1);
                return;
            }

            var group = new TransformGroup();
            group.Children.Add(_target.RenderTransform);
            group.Children.Add(new ScaleTransform(1, 1));
            _target.RenderTransform = group;
        }

        // ── Token resolution ─────────────────────────────────────────

        private TimeSpan ResolveDuration()
        {
            if (_target.TryFindResource("FluentDurationNormal") is Duration d && d.HasTimeSpan)
                return d.TimeSpan;
            return TimeSpan.FromMilliseconds(167);
        }

        private IEasingFunction? ResolveEase() =>
            _target.TryFindResource("FluentEasePoint") as IEasingFunction;

        // ── Brush comparison ─────────────────────────────────────────

        private static bool BrushesEqual(Brush? a, Brush? b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a is null || b is null)
                return false;
            if (a is SolidColorBrush scbA && b is SolidColorBrush scbB)
                return scbA.Color == scbB.Color && scbA.Opacity == scbB.Opacity;
            return false;
        }
    }
}
