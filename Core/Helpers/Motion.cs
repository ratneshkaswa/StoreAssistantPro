using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Lightweight attached behaviors that apply Fluent 2 motion to any
/// <see cref="FrameworkElement"/>.  Each behavior is a single boolean
/// attached property — no code-behind, no custom controls.
/// <para>
/// All animations consume tokens from <c>DesignSystem.xaml</c>
/// (durations, easing curves, scale factors, slide offsets) so that
/// timing changes are centralised.
/// </para>
///
/// <para><b>Available behaviors:</b></para>
/// <list type="table">
///   <listheader><term>Property</term><description>Effect</description></listheader>
///   <item>
///     <term><see cref="FadeInProperty"/></term>
///     <description>Fade from 0 → 1 on <c>Loaded</c> (FluentDurationSlow, Decelerate).</description>
///   </item>
///   <item>
///     <term><see cref="FadeOutProperty"/></term>
///     <description>Fade from 1 → 0 on <c>Unloaded</c> (FluentDurationNormal, Accelerate).
///     Best-effort — WPF may remove the element before the animation completes.</description>
///   </item>
///   <item>
///     <term><see cref="ScaleHoverProperty"/></term>
///     <description>Subtle 0.985 → 1.0 scale pulse on <c>MouseEnter</c> /
///     <c>MouseLeave</c> (FluentDurationFast, Point curve).</description>
///   </item>
///   <item>
///     <term><see cref="SlideFadeInProperty"/></term>
///     <description>Slide-up 12 px + fade from 0 → 1 on <c>Loaded</c>
///     (FluentDurationSlow, Decelerate).</description>
///   </item>
/// </list>
///
/// <para><b>Usage (XAML):</b></para>
/// <code>
/// &lt;Border h:Motion.FadeIn="True" …/&gt;
/// &lt;Border h:Motion.SlideFadeIn="True" …/&gt;
/// &lt;Border h:Motion.ScaleHover="True" …/&gt;
/// </code>
/// </summary>
public static class Motion
{
    // ═══════════════════════════════════════════════════════════════════
    //  FADE IN  —  Loaded → Opacity 0 → 1
    // ═══════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty FadeInProperty =
        DependencyProperty.RegisterAttached(
            "FadeIn", typeof(bool), typeof(Motion),
            new PropertyMetadata(false, OnFadeInChanged));

    public static bool GetFadeIn(DependencyObject obj) =>
        (bool)obj.GetValue(FadeInProperty);

    public static void SetFadeIn(DependencyObject obj, bool value) =>
        obj.SetValue(FadeInProperty, value);

    private static void OnFadeInChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not true)
            return;

        if (IsMotionDisabled(fe))
            return;

        fe.Opacity = 0;
        fe.Loaded += static (sender, _) =>
        {
            var el = (FrameworkElement)sender;
            var duration = GetDuration(el, "FluentDurationSlow", TimeSpan.FromMilliseconds(250));
            if (duration == TimeSpan.Zero) { el.Opacity = 1; return; }
            var ease = TryFindEase(el, "FluentEaseDecelerate");

            var anim = new DoubleAnimation(0, 1, new Duration(duration))
            {
                EasingFunction = ease
            };
            anim.Freeze();
            el.BeginAnimation(UIElement.OpacityProperty, anim);
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FADE OUT  —  Unloaded → Opacity 1 → 0  (best-effort)
    // ═══════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty FadeOutProperty =
        DependencyProperty.RegisterAttached(
            "FadeOut", typeof(bool), typeof(Motion),
            new PropertyMetadata(false, OnFadeOutChanged));

    public static bool GetFadeOut(DependencyObject obj) =>
        (bool)obj.GetValue(FadeOutProperty);

    public static void SetFadeOut(DependencyObject obj, bool value) =>
        obj.SetValue(FadeOutProperty, value);

    private static void OnFadeOutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not true)
            return;

        if (IsMotionDisabled(fe))
            return;

        fe.Unloaded += static (sender, _) =>
        {
            var el = (FrameworkElement)sender;
            var duration = GetDuration(el, "FluentDurationNormal", TimeSpan.FromMilliseconds(167));
            if (duration == TimeSpan.Zero) return;
            var ease = TryFindEase(el, "FluentEaseAccelerate");

            var anim = new DoubleAnimation(el.Opacity, 0, new Duration(duration))
            {
                EasingFunction = ease
            };
            anim.Freeze();
            el.BeginAnimation(UIElement.OpacityProperty, anim);
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    //  SCALE HOVER  —  MouseEnter/Leave → subtle 0.985 → 1.0 pulse
    // ═══════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty ScaleHoverProperty =
        DependencyProperty.RegisterAttached(
            "ScaleHover", typeof(bool), typeof(Motion),
            new PropertyMetadata(false, OnScaleHoverChanged));

    public static bool GetScaleHover(DependencyObject obj) =>
        (bool)obj.GetValue(ScaleHoverProperty);

    public static void SetScaleHover(DependencyObject obj, bool value) =>
        obj.SetValue(ScaleHoverProperty, value);

    private static void OnScaleHoverChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not true)
            return;

        if (IsMotionDisabled(fe))
            return;

        EnsureScaleTransform(fe);

        fe.MouseEnter += static (sender, _) =>
        {
            var el = (FrameworkElement)sender;
            AnimateScale(el, 1.0);
        };

        fe.MouseLeave += static (sender, _) =>
        {
            var el = (FrameworkElement)sender;
            var from = TryFindDouble(el, "MotionScaleHoverFrom", 0.985);
            AnimateScale(el, from);
        };

        // Start at resting scale
        fe.Loaded += static (sender, _) =>
        {
            var el = (FrameworkElement)sender;
            var from = TryFindDouble(el, "MotionScaleHoverFrom", 0.985);
            if (el.RenderTransform is ScaleTransform st)
            {
                st.ScaleX = from;
                st.ScaleY = from;
            }
        };
    }

    private static void AnimateScale(FrameworkElement el, double to)
    {
        if (el.RenderTransform is not ScaleTransform st)
            return;

        var duration = GetDuration(el, "FluentDurationFast", TimeSpan.FromMilliseconds(83));
        if (duration == TimeSpan.Zero) { st.ScaleX = to; st.ScaleY = to; return; }
        var ease = TryFindEase(el, "FluentEasePoint");

        var animX = new DoubleAnimation(to, new Duration(duration)) { EasingFunction = ease };
        var animY = new DoubleAnimation(to, new Duration(duration)) { EasingFunction = ease };
        animX.Freeze();
        animY.Freeze();

        st.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
        st.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  SLIDE FADE IN  —  Loaded → TranslateY offset→0 + Opacity 0→1
    // ═══════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty SlideFadeInProperty =
        DependencyProperty.RegisterAttached(
            "SlideFadeIn", typeof(bool), typeof(Motion),
            new PropertyMetadata(false, OnSlideFadeInChanged));

    public static bool GetSlideFadeIn(DependencyObject obj) =>
        (bool)obj.GetValue(SlideFadeInProperty);

    public static void SetSlideFadeIn(DependencyObject obj, bool value) =>
        obj.SetValue(SlideFadeInProperty, value);

    private static void OnSlideFadeInChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not true)
            return;

        if (IsMotionDisabled(fe))
            return;

        EnsureTranslateTransform(fe);
        fe.Opacity = 0;

        fe.Loaded += static (sender, _) =>
        {
            var el = (FrameworkElement)sender;
            var duration = GetDuration(el, "FluentDurationSlow", TimeSpan.FromMilliseconds(250));
            if (duration == TimeSpan.Zero) { el.Opacity = 1; return; }
            var ease = TryFindEase(el, "FluentEaseDecelerate");
            var offset = TryFindDouble(el, "MotionSlideOffsetSmall", 12);

            // Opacity
            var fadeAnim = new DoubleAnimation(0, 1, new Duration(duration))
            {
                EasingFunction = ease
            };
            fadeAnim.Freeze();
            el.BeginAnimation(UIElement.OpacityProperty, fadeAnim);

            // TranslateY
            if (el.RenderTransform is TranslateTransform tt)
            {
                tt.Y = offset;
                var slideAnim = new DoubleAnimation(offset, 0, new Duration(duration))
                {
                    EasingFunction = ease
                };
                slideAnim.Freeze();
                tt.BeginAnimation(TranslateTransform.YProperty, slideAnim);
            }
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    //  PAGE FADE IN  —  Loaded → Opacity 0 → 1 (150 ms, no movement)
    //
    //  Designed for the implicit UserControl style so every page view
    //  automatically fades in when navigated to.  Uses the dedicated
    //  FluentDurationPageFade token (150 ms) for a quick, subtle
    //  entrance that layers with the container-level workspace
    //  transition without competing visually.
    //
    //  No slide / translate is applied — if the device or scenario is
    //  performance-constrained, the fade-only approach has zero layout
    //  cost (Opacity is GPU-composited).
    // ═══════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty PageFadeInProperty =
        DependencyProperty.RegisterAttached(
            "PageFadeIn", typeof(bool), typeof(Motion),
            new PropertyMetadata(false, OnPageFadeInChanged));

    public static bool GetPageFadeIn(DependencyObject obj) =>
        (bool)obj.GetValue(PageFadeInProperty);

    public static void SetPageFadeIn(DependencyObject obj, bool value) =>
        obj.SetValue(PageFadeInProperty, value);

    private static void OnPageFadeInChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not true)
            return;

        if (IsMotionDisabled(fe))
            return;

        fe.Opacity = 0;
        fe.Loaded += static (sender, _) =>
        {
            var el = (FrameworkElement)sender;
            var duration = GetDuration(el, "FluentDurationPageFade", TimeSpan.FromMilliseconds(150));
            if (duration == TimeSpan.Zero) { el.Opacity = 1; return; }
            var ease = TryFindEase(el, "FluentEaseDecelerate");

            var anim = new DoubleAnimation(0, 1, new Duration(duration))
            {
                EasingFunction = ease
            };
            anim.Freeze();
            el.BeginAnimation(UIElement.OpacityProperty, anim);
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    //  WINDOW FADE IN  —  Loaded → Opacity 0 → 1 (250 ms)
    //
    //  For Windows (login, setup) that don't get the UserControl
    //  implicit PageFadeIn.  Apply on the Window element itself.
    // ═══════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty WindowFadeInProperty =
        DependencyProperty.RegisterAttached(
            "WindowFadeIn", typeof(bool), typeof(Motion),
            new PropertyMetadata(false, OnWindowFadeInChanged));

    public static bool GetWindowFadeIn(DependencyObject obj) =>
        (bool)obj.GetValue(WindowFadeInProperty);

    public static void SetWindowFadeIn(DependencyObject obj, bool value) =>
        obj.SetValue(WindowFadeInProperty, value);

    private static void OnWindowFadeInChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window w || e.NewValue is not true)
            return;

        if (IsMotionDisabled(w))
            return;

        w.Opacity = 0;
        w.ContentRendered += static (sender, _) =>
        {
            var win = (Window)sender;
            var duration = GetDuration(win, "FluentDurationSlow", TimeSpan.FromMilliseconds(250));
            if (duration == TimeSpan.Zero) { win.Opacity = 1; return; }
            var ease = TryFindEase(win, "FluentEaseDecelerate");

            var anim = new DoubleAnimation(0, 1, new Duration(duration))
            {
                EasingFunction = ease
            };
            anim.Freeze();
            win.BeginAnimation(UIElement.OpacityProperty, anim);
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    //  SLIDE FADE REVEAL  —  Visibility → slide-up + fade on show
    //
    //  For inline forms (Add/Edit) that toggle Visibility via binding.
    //  Plays entrance animation each time the element becomes Visible.
    // ═══════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty SlideFadeRevealProperty =
        DependencyProperty.RegisterAttached(
            "SlideFadeReveal", typeof(bool), typeof(Motion),
            new PropertyMetadata(false, OnSlideFadeRevealChanged));

    public static bool GetSlideFadeReveal(DependencyObject obj) =>
        (bool)obj.GetValue(SlideFadeRevealProperty);

    public static void SetSlideFadeReveal(DependencyObject obj, bool value) =>
        obj.SetValue(SlideFadeRevealProperty, value);

    private static void OnSlideFadeRevealChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not true)
            return;

        if (IsMotionDisabled(fe))
            return;

        EnsureTranslateTransform(fe);

        fe.IsVisibleChanged += static (sender, args) =>
        {
            if (args.NewValue is not true)
                return;

            var el = (FrameworkElement)sender;
            var duration = GetDuration(el, "FluentDurationSlow", TimeSpan.FromMilliseconds(250));
            if (duration == TimeSpan.Zero) return;
            var ease = TryFindEase(el, "FluentEaseDecelerate");
            var offset = TryFindDouble(el, "MotionSlideOffsetSmall", 12);

            el.Opacity = 0;
            var fadeAnim = new DoubleAnimation(0, 1, new Duration(duration))
            {
                EasingFunction = ease
            };
            fadeAnim.Freeze();
            el.BeginAnimation(UIElement.OpacityProperty, fadeAnim);

            if (el.RenderTransform is TranslateTransform tt)
            {
                tt.Y = offset;
                var slideAnim = new DoubleAnimation(offset, 0, new Duration(duration))
                {
                    EasingFunction = ease
                };
                slideAnim.Freeze();
                tt.BeginAnimation(TranslateTransform.YProperty, slideAnim);
            }
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    //  STAGGER INDEX  —  Loaded → delay = index × 50 ms, then fade in
    //
    //  Set on repeated items (stat cards, task items) to create a
    //  sequential cascade entrance.
    //    <Border h:Motion.StaggerIndex="0" … />
    //    <Border h:Motion.StaggerIndex="1" … />
    //  Value of -1 means disabled.
    // ═══════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty StaggerIndexProperty =
        DependencyProperty.RegisterAttached(
            "StaggerIndex", typeof(int), typeof(Motion),
            new PropertyMetadata(-1, OnStaggerIndexChanged));

    public static int GetStaggerIndex(DependencyObject obj) =>
        (int)obj.GetValue(StaggerIndexProperty);

    public static void SetStaggerIndex(DependencyObject obj, int value) =>
        obj.SetValue(StaggerIndexProperty, value);

    private static void OnStaggerIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not int index || index < 0)
            return;

        if (IsMotionDisabled(fe))
            return;

        EnsureTranslateTransform(fe);
        fe.Opacity = 0;

        fe.Loaded += static (sender, _) =>
        {
            var el = (FrameworkElement)sender;
            var idx = GetStaggerIndex(el);
            var delay = TimeSpan.FromMilliseconds(idx * 50);
            var duration = GetDuration(el, "FluentDurationSlow", TimeSpan.FromMilliseconds(250));
            if (duration == TimeSpan.Zero) { el.Opacity = 1; return; }
            var ease = TryFindEase(el, "FluentEaseDecelerate");
            var offset = TryFindDouble(el, "MotionSlideOffsetSmall", 12);

            var fadeAnim = new DoubleAnimation(0, 1, new Duration(duration))
            {
                EasingFunction = ease,
                BeginTime = delay
            };
            el.BeginAnimation(UIElement.OpacityProperty, fadeAnim);

            if (el.RenderTransform is TranslateTransform tt)
            {
                tt.Y = offset;
                var slideAnim = new DoubleAnimation(offset, 0, new Duration(duration))
                {
                    EasingFunction = ease,
                    BeginTime = delay
                };
                tt.BeginAnimation(TranslateTransform.YProperty, slideAnim);
            }
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    //  SHAKE ERROR  —  triggers a horizontal shake animation
    //
    //  Bind to a bool (e.g. ViewModel.HasError) — plays each time
    //  the value transitions to True.
    // ═══════════════════════════════════════════════════════════════════

    public static readonly DependencyProperty ShakeOnTriggerProperty =
        DependencyProperty.RegisterAttached(
            "ShakeOnTrigger", typeof(bool), typeof(Motion),
            new PropertyMetadata(false, OnShakeOnTriggerChanged));

    public static bool GetShakeOnTrigger(DependencyObject obj) =>
        (bool)obj.GetValue(ShakeOnTriggerProperty);

    public static void SetShakeOnTrigger(DependencyObject obj, bool value) =>
        obj.SetValue(ShakeOnTriggerProperty, value);

    private static void OnShakeOnTriggerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not true)
            return;

        if (IsMotionDisabled(fe))
        {
            fe.Dispatcher.BeginInvoke(() => fe.SetCurrentValue(ShakeOnTriggerProperty, false));
            return;
        }

        EnsureTranslateTransform(fe);

        if (fe.RenderTransform is TranslateTransform tt)
        {
            var anim = new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(300))
            };
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0)));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(-6, KeyTime.FromPercent(0.15)));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(5, KeyTime.FromPercent(0.3)));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(-4, KeyTime.FromPercent(0.45)));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(3, KeyTime.FromPercent(0.6)));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(-1, KeyTime.FromPercent(0.8)));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1.0)));

            tt.BeginAnimation(TranslateTransform.XProperty, anim);
        }

        // Reset to false so it can trigger again
        fe.Dispatcher.BeginInvoke(() => fe.SetCurrentValue(ShakeOnTriggerProperty, false));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Shared helpers
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns <c>true</c> when animation durations are zero in the design
    /// system, meaning all motion should be skipped entirely.
    /// </summary>
    private static bool IsMotionDisabled(FrameworkElement fe) =>
        GetDuration(fe, "FluentDurationSlow", TimeSpan.FromMilliseconds(250)) == TimeSpan.Zero;

    /// <summary>
    /// Resolves a <see cref="Duration"/> resource from the element's tree,
    /// falling back to a compile-time default.
    /// </summary>
    private static TimeSpan GetDuration(FrameworkElement fe, string key, TimeSpan fallback)
    {
        if (fe.TryFindResource(key) is Duration d && d.HasTimeSpan)
            return d.TimeSpan;
        return fallback;
    }

    /// <summary>
    /// Resolves an <see cref="IEasingFunction"/> resource, returning
    /// <c>null</c> (linear) if not found.
    /// </summary>
    private static IEasingFunction? TryFindEase(FrameworkElement fe, string key) =>
        fe.TryFindResource(key) as IEasingFunction;

    /// <summary>
    /// Resolves a <see cref="double"/> resource, with fallback.
    /// </summary>
    private static double TryFindDouble(FrameworkElement fe, string key, double fallback) =>
        fe.TryFindResource(key) is double v ? v : fallback;

    /// <summary>
    /// Ensures the element has a <see cref="ScaleTransform"/> centered
    /// at 50 %/50 % so scale animations pivot from the middle.
    /// Preserves any existing transform by composing into a group.
    /// </summary>
    private static void EnsureScaleTransform(FrameworkElement fe)
    {
        fe.RenderTransformOrigin = new Point(0.5, 0.5);

        if (fe.RenderTransform is ScaleTransform)
            return;

        if (IsIdentityOrNull(fe.RenderTransform))
        {
            fe.RenderTransform = new ScaleTransform(1, 1);
            return;
        }

        // Existing non-identity transform — wrap in a group
        var group = new TransformGroup();
        group.Children.Add(fe.RenderTransform);
        group.Children.Add(new ScaleTransform(1, 1));
        fe.RenderTransform = group;
    }

    /// <summary>
    /// Ensures the element has a <see cref="TranslateTransform"/>
    /// for slide animations. Preserves any existing transform.
    /// </summary>
    private static void EnsureTranslateTransform(FrameworkElement fe)
    {
        if (fe.RenderTransform is TranslateTransform)
            return;

        if (IsIdentityOrNull(fe.RenderTransform))
        {
            fe.RenderTransform = new TranslateTransform(0, 0);
            return;
        }

        var group = new TransformGroup();
        group.Children.Add(fe.RenderTransform);
        group.Children.Add(new TranslateTransform(0, 0));
        fe.RenderTransform = group;
    }

    /// <summary>
    /// Returns <c>true</c> when the transform is null or the default
    /// identity matrix (WPF's initial <c>RenderTransform</c> state).
    /// </summary>
    private static bool IsIdentityOrNull(Transform? transform) =>
        transform is null || (transform is MatrixTransform mt && mt.Matrix.IsIdentity);
}
