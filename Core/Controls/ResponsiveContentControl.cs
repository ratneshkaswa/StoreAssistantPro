using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// A <see cref="ContentControl"/> designed for the MainWindow content region
/// that stretches its content to fill available space while enabling vertical
/// scrolling when the content's desired height exceeds the viewport.
/// <para>
/// The default style (defined in <c>App.xaml</c>) wraps the content presenter
/// inside a <see cref="ScrollViewer"/> and a <see cref="ViewportConstrainedPanel"/>
/// that passes the viewport size as the measure constraint — preserving
/// star-sized Grid rows inside hosted views.
/// </para>
///
/// <para><b>Workspace transition animation:</b></para>
/// <para>
/// Each time <see cref="ContentControl.Content"/> changes (page navigation,
/// billing mode toggle, etc.) the control plays a combined fade-in + optional
/// slide-up animation on the template's <c>PART_ScrollViewer</c> and
/// <c>PART_SlideTransform</c>.  The animation is purely visual — the new
/// content is immediately hit-testable and focusable, so there is zero
/// interaction delay.
/// </para>
/// <para>
/// The slide effect is controlled by <see cref="EnableSlideTransition"/>.
/// Set to <c>False</c> to keep only the fade.  Durations and easing come
/// from <c>DesignSystem.xaml</c> tokens resolved at runtime.
/// </para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;controls:ResponsiveContentControl
///     Content="{Binding CurrentView}"
///     EnableSlideTransition="True"/&gt;
/// </code>
/// </summary>
public class ResponsiveContentControl : ContentControl
{
    // ── Template part names ──────────────────────────────────────────
    private const string PartScrollViewer = "PART_ScrollViewer";
    private const string PartSlideTransform = "PART_SlideTransform";

    // ── Cached template parts ────────────────────────────────────────
    private ScrollViewer? _scrollViewer;
    private TranslateTransform? _slideTransform;

    // ── First load guard ─────────────────────────────────────────────
    private bool _hasAppliedTemplate;
    private bool _isFirstContent = true;

    // ── EnableSlideTransition DP ─────────────────────────────────────

    /// <summary>
    /// When <c>True</c>, content transitions include a subtle slide-up
    /// in addition to the fade.  Defaults to <c>True</c>.
    /// </summary>
    public static readonly DependencyProperty EnableSlideTransitionProperty =
        DependencyProperty.Register(
            nameof(EnableSlideTransition),
            typeof(bool),
            typeof(ResponsiveContentControl),
            new PropertyMetadata(true));

    public bool EnableSlideTransition
    {
        get => (bool)GetValue(EnableSlideTransitionProperty);
        set => SetValue(EnableSlideTransitionProperty, value);
    }

    // ── Constructor ──────────────────────────────────────────────────

    static ResponsiveContentControl()
    {
        FocusableProperty.OverrideMetadata(
            typeof(ResponsiveContentControl),
            new FrameworkPropertyMetadata(false));
    }

    // ── Template wiring ──────────────────────────────────────────────

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _scrollViewer = GetTemplateChild(PartScrollViewer) as ScrollViewer;
        _slideTransform = GetTemplateChild(PartSlideTransform) as TranslateTransform;
        _hasAppliedTemplate = true;
    }

    // ── Content change → trigger transition ──────────────────────────

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);

        // Skip the initial binding — the template's Loaded EventTrigger
        // already handles the very first fade-in.
        if (_isFirstContent)
        {
            _isFirstContent = false;
            return;
        }

        if (!_hasAppliedTemplate || _scrollViewer is null)
            return;

        PlayTransition();
    }

    // ── Animation ────────────────────────────────────────────────────

    private void PlayTransition()
    {
        var duration = ResolveDuration("FluentDurationSlow", 250);
        var ease = TryResolveEase("FluentEaseDecelerate");

        // ── Fade: Opacity 0 → 1 ─────────────────────────────────────
        var fadeAnim = new DoubleAnimation(0, 1, duration)
        {
            EasingFunction = ease
        };
        fadeAnim.Freeze();
        _scrollViewer!.BeginAnimation(OpacityProperty, fadeAnim);

        // ── Slide: TranslateY offset → 0 ────────────────────────────
        if (EnableSlideTransition && _slideTransform is not null)
        {
            var offset = TryResolveDouble("MotionSlideOffsetSmall", 12);
            var slideAnim = new DoubleAnimation(offset, 0, duration)
            {
                EasingFunction = ease
            };
            slideAnim.Freeze();
            _slideTransform.BeginAnimation(TranslateTransform.YProperty, slideAnim);
        }

        // Reset scroll position to top for the new content
        _scrollViewer!.ScrollToTop();
    }

    // ── Token resolution helpers ─────────────────────────────────────

    private Duration ResolveDuration(string key, double fallbackMs)
    {
        if (TryFindResource(key) is Duration d && d.HasTimeSpan)
            return d;
        return new Duration(TimeSpan.FromMilliseconds(fallbackMs));
    }

    private IEasingFunction? TryResolveEase(string key) =>
        TryFindResource(key) as IEasingFunction;

    private double TryResolveDouble(string key, double fallback) =>
        TryFindResource(key) is double v ? v : fallback;
}
