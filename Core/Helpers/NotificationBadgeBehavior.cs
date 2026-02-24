using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that manages a notification count badge on any
/// <see cref="Panel"/>. The badge is a visual overlay positioned at
/// the top-right corner.
/// <para>
/// <b>Auto-update:</b> Bind <see cref="CountProperty"/> to
/// <c>AppState.UnreadNotificationCount</c>. The badge automatically
/// collapses when count reaches zero and reappears when count &gt; 0.
/// </para>
/// <para>
/// <b>Animation:</b> When the count <em>increases</em>, two
/// non-repeating micro-animations play:
/// <list type="bullet">
///   <item><b>Bell pulse</b> — the parent panel scales 1.0 → 1.15 → 1.0
///         (FluentDurationSlow, FluentEasePoint). A single pulse to
///         draw the eye without sustained motion.</item>
///   <item><b>Badge entrance</b> — when the badge first appears
///         (count was 0), it fades + scales in from 0 → 1
///         (FluentDurationNormal, FluentEaseDecelerate). When the count
///         merely changes (already visible), a quick opacity blink
///         confirms the update.</item>
/// </list>
/// No animation plays when the count decreases or stays the same —
/// dismissals should feel quiet.
/// </para>
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;Grid h:NotificationBadgeBehavior.Count="{Binding AppState.UnreadNotificationCount}"&gt;
///     &lt;TextBlock Text="🔔" FontSize="18"/&gt;
/// &lt;/Grid&gt;
/// </code>
/// </summary>
public static class NotificationBadgeBehavior
{
    // ── Count attached property ───────────────────────────────────

    /// <summary>
    /// The unread notification count. When &gt; 0 the badge is shown;
    /// when 0 it collapses.
    /// </summary>
    public static readonly DependencyProperty CountProperty =
        DependencyProperty.RegisterAttached(
            "Count",
            typeof(int),
            typeof(NotificationBadgeBehavior),
            new PropertyMetadata(0, OnCountChanged));

    public static int GetCount(DependencyObject obj) =>
        (int)obj.GetValue(CountProperty);

    public static void SetCount(DependencyObject obj, int value) =>
        obj.SetValue(CountProperty, value);

    // ── BadgeBackground attached property ─────────────────────────

    /// <summary>
    /// Background brush for the badge circle. Defaults to red (#E53935).
    /// </summary>
    public static readonly DependencyProperty BadgeBackgroundProperty =
        DependencyProperty.RegisterAttached(
            "BadgeBackground",
            typeof(Brush),
            typeof(NotificationBadgeBehavior),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35)), OnAppearanceChanged));

    public static Brush GetBadgeBackground(DependencyObject obj) =>
        (Brush)obj.GetValue(BadgeBackgroundProperty);

    public static void SetBadgeBackground(DependencyObject obj, Brush value) =>
        obj.SetValue(BadgeBackgroundProperty, value);

    // ── BadgeForeground attached property ─────────────────────────

    /// <summary>
    /// Foreground brush for the badge count text. Defaults to White.
    /// </summary>
    public static readonly DependencyProperty BadgeForegroundProperty =
        DependencyProperty.RegisterAttached(
            "BadgeForeground",
            typeof(Brush),
            typeof(NotificationBadgeBehavior),
            new PropertyMetadata(Brushes.White, OnAppearanceChanged));

    public static Brush GetBadgeForeground(DependencyObject obj) =>
        (Brush)obj.GetValue(BadgeForegroundProperty);

    public static void SetBadgeForeground(DependencyObject obj, Brush value) =>
        obj.SetValue(BadgeForegroundProperty, value);

    // ── Private: badge element stored on the target ───────────────

    private static readonly DependencyProperty BadgeElementProperty =
        DependencyProperty.RegisterAttached(
            "BadgeElement",
            typeof(Border),
            typeof(NotificationBadgeBehavior));

    // ── Change handlers ───────────────────────────────────────────

    private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Panel panel)
            return;

        var oldCount = (int)e.OldValue;
        var newCount = (int)e.NewValue;
        var badge = GetOrCreateBadge(panel);

        UpdateBadge(badge, newCount, panel);

        // Animate only when count *increases* — dismissals stay quiet
        if (newCount > oldCount && newCount > 0)
        {
            var wasHidden = oldCount == 0;
            PlayBellPulse(panel);
            PlayBadgeEntrance(badge, wasHidden);
        }
    }

    private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Panel panel)
            return;

        var badge = (Border?)panel.GetValue(BadgeElementProperty);
        if (badge is null)
            return;

        badge.Background = GetBadgeBackground(panel);
        if (badge.Child is TextBlock tb)
            tb.Foreground = GetBadgeForeground(panel);
    }

    // ── Badge creation ────────────────────────────────────────────

    private static Border GetOrCreateBadge(Panel panel)
    {
        var existing = (Border?)panel.GetValue(BadgeElementProperty);
        if (existing is not null)
            return existing;

        var textBlock = new TextBlock
        {
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = GetBadgeForeground(panel),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var badge = new Border
        {
            Background = GetBadgeBackground(panel),
            CornerRadius = new CornerRadius(8),
            MinWidth = 16,
            Height = 16,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -4, -4, 0),
            Padding = new Thickness(4, 0, 4, 0),
            IsHitTestVisible = false,
            Child = textBlock,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new ScaleTransform(1, 1)
        };

        panel.Children.Add(badge);
        panel.SetValue(BadgeElementProperty, badge);

        return badge;
    }

    // ── Badge update ──────────────────────────────────────────────

    private static void UpdateBadge(Border badge, int count, Panel panel)
    {
        if (badge.Child is TextBlock tb)
        {
            tb.Text = count > 99 ? "99+" : count.ToString();
            tb.Foreground = GetBadgeForeground(panel);
        }

        badge.Background = GetBadgeBackground(panel);
        badge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // ═══════════════════════════════════════════════════════════════
    //  ANIMATIONS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Single scale pulse on the parent panel (the bell icon container):
    /// 1.0 → 1.15 → 1.0 over FluentDurationSlow with FluentEasePoint.
    /// </summary>
    private static void PlayBellPulse(Panel panel)
    {
        EnsurePanelScaleTransform(panel);
        if (panel.RenderTransform is not ScaleTransform st)
            return;

        var duration = ResolveDuration(panel, "FluentDurationSlow", 250);
        var ease = ResolveEase(panel, "FluentEasePoint");

        // Two-keyframe: rest → peak → rest
        var halfDuration = TimeSpan.FromMilliseconds(duration.TotalMilliseconds / 2);

        var animX = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
        animX.KeyFrames.Add(new EasingDoubleKeyFrame(1.15, KeyTime.FromTimeSpan(halfDuration), ease));
        animX.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(duration), ease));
        animX.Freeze();

        var animY = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
        animY.KeyFrames.Add(new EasingDoubleKeyFrame(1.15, KeyTime.FromTimeSpan(halfDuration), ease));
        animY.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(duration), ease));
        animY.Freeze();

        st.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
        st.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
    }

    /// <summary>
    /// Badge entrance: if <paramref name="firstAppearance"/> is true,
    /// scale + fade from 0 → 1; otherwise just a quick opacity blink
    /// (1 → 0.4 → 1) to confirm the count updated.
    /// </summary>
    private static void PlayBadgeEntrance(Border badge, bool firstAppearance)
    {
        if (badge.RenderTransform is not ScaleTransform badgeSt)
            return;

        if (firstAppearance)
        {
            var duration = ResolveDuration(badge, "FluentDurationNormal", 167);
            var ease = ResolveEase(badge, "FluentEaseDecelerate");

            // Scale from 0 → 1
            var scaleX = new DoubleAnimation(0, 1, new Duration(duration))
            {
                EasingFunction = ease,
                FillBehavior = FillBehavior.Stop
            };
            scaleX.Freeze();
            var scaleY = new DoubleAnimation(0, 1, new Duration(duration))
            {
                EasingFunction = ease,
                FillBehavior = FillBehavior.Stop
            };
            scaleY.Freeze();

            // Fade from 0 → 1
            var fade = new DoubleAnimation(0, 1, new Duration(duration))
            {
                EasingFunction = ease,
                FillBehavior = FillBehavior.Stop
            };
            fade.Freeze();

            badgeSt.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            badgeSt.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
            badge.BeginAnimation(UIElement.OpacityProperty, fade);
        }
        else
        {
            // Quick blink: 1 → 0.4 → 1
            var duration = ResolveDuration(badge, "FluentDurationNormal", 167);
            var ease = ResolveEase(badge, "FluentEasePoint");

            var blink = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
            var half = TimeSpan.FromMilliseconds(duration.TotalMilliseconds / 2);
            blink.KeyFrames.Add(new EasingDoubleKeyFrame(0.4, KeyTime.FromTimeSpan(half), ease));
            blink.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(duration), ease));
            blink.Freeze();

            badge.BeginAnimation(UIElement.OpacityProperty, blink);
        }
    }

    // ── Transform helpers ─────────────────────────────────────────

    private static void EnsurePanelScaleTransform(Panel panel)
    {
        panel.RenderTransformOrigin = new Point(0.5, 0.5);

        if (panel.RenderTransform is ScaleTransform)
            return;

        if (panel.RenderTransform is null or MatrixTransform)
        {
            panel.RenderTransform = new ScaleTransform(1, 1);
            return;
        }

        var group = new TransformGroup();
        group.Children.Add(panel.RenderTransform);
        group.Children.Add(new ScaleTransform(1, 1));
        panel.RenderTransform = group;
    }

    // ── Token resolution ──────────────────────────────────────────

    private static TimeSpan ResolveDuration(FrameworkElement fe, string key, double fallbackMs)
    {
        if (fe.TryFindResource(key) is Duration d && d.HasTimeSpan)
            return d.TimeSpan;
        return TimeSpan.FromMilliseconds(fallbackMs);
    }

    private static IEasingFunction? ResolveEase(FrameworkElement fe, string key) =>
        fe.TryFindResource(key) as IEasingFunction;
}
