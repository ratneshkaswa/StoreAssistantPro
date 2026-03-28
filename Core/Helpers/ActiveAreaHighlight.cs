using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that gives the active workspace a subtle elevation
/// increase and brightness boost when the billing focus lock activates.
/// <para>
/// <b>Visual effect:</b> The workspace's <see cref="UIElement.Effect"/>
/// (drop shadow) deepens from Level 1 (resting) to Level 2 (active), and
/// a faint white overlay tints the <see cref="Panel.Background"/> to create
/// a perceived brightness lift. Both transitions are smooth and subtle —
/// the intent is to draw focus without drama, using direct state
/// changes instead of transition-heavy animations.
/// </para>
///
/// <para><b>Layering with other behaviors:</b></para>
/// <list type="table">
///   <listheader><term>Behavior</term><description>Property animated</description></listheader>
///   <item><term>BillingDimBehavior</term><description><c>Opacity</c></description></item>
///   <item><term>AdaptiveWorkspace</term><description><c>BorderBrush.Color</c> / <c>LayoutTransform.ScaleY</c></description></item>
///   <item><term>ActiveAreaHighlight</term><description><c>Effect</c> (shadow) + <c>Background.Color</c> (tint)</description></item>
/// </list>
/// <para>
/// No property collisions — each behavior targets a distinct visual channel.
/// </para>
///
/// <para><b>Properties:</b></para>
/// <list type="bullet">
///   <item><see cref="IsActiveProperty"/> — Master driver (bind to
///         <c>FocusLock.IsFocusLocked</c>).</item>
///   <item><see cref="EnableElevationProperty"/> — Animates shadow
///         from resting to active depth.</item>
///   <item><see cref="EnableBrightnessProperty"/> — Animates a faint
///         white overlay into the background.</item>
/// </list>
///
/// <para><b>Usage (MainWindow.xaml):</b></para>
/// <code>
/// &lt;controls:ResponsiveContentControl
///     h:ActiveAreaHighlight.EnableElevation="True"
///     h:ActiveAreaHighlight.EnableBrightness="True"
///     h:ActiveAreaHighlight.IsActive="{Binding FocusLock.IsFocusLocked}"
///     .../&gt;
/// </code>
/// </summary>
public static class ActiveAreaHighlight
{
    // ── Shadow depth constants ────────────────────────────────────

    /// <summary>Resting state: same as FluentShadowSmall.</summary>
    private const double RestingBlur = 8;
    private const double RestingDepth = 2;
    private const double RestingOpacity = 0.08;

    /// <summary>Active state: between Level 1 and Level 2 — subtle lift.</summary>
    private const double ActiveBlur = 12;
    private const double ActiveDepth = 3;
    private const double ActiveOpacity = 0.12;

    /// <summary>Active brightness tint — nearly transparent white.</summary>
    private static readonly Color ActiveTint = Color.FromArgb(0x08, 0xFF, 0xFF, 0xFF);

    // ── IsActive DP (master driver) ──────────────────────────────

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.RegisterAttached(
            "IsActive",
            typeof(bool),
            typeof(ActiveAreaHighlight),
            new PropertyMetadata(false, OnIsActiveChanged));

    public static bool GetIsActive(DependencyObject obj) =>
        (bool)obj.GetValue(IsActiveProperty);

    public static void SetIsActive(DependencyObject obj, bool value) =>
        obj.SetValue(IsActiveProperty, value);

    // ── EnableElevation DP ───────────────────────────────────────

    /// <summary>
    /// When <c>True</c>, the element's drop shadow smoothly
    /// transitions between resting and active depth levels.
    /// </summary>
    public static readonly DependencyProperty EnableElevationProperty =
        DependencyProperty.RegisterAttached(
            "EnableElevation",
            typeof(bool),
            typeof(ActiveAreaHighlight),
            new PropertyMetadata(false, OnEnableElevationChanged));

    public static bool GetEnableElevation(DependencyObject obj) =>
        (bool)obj.GetValue(EnableElevationProperty);

    public static void SetEnableElevation(DependencyObject obj, bool value) =>
        obj.SetValue(EnableElevationProperty, value);

    // ── EnableBrightness DP ──────────────────────────────────────

    /// <summary>
    /// When <c>True</c>, a faint white overlay animates into
    /// the element's <see cref="Panel.Background"/> to create a
    /// subtle brightness boost.
    /// </summary>
    public static readonly DependencyProperty EnableBrightnessProperty =
        DependencyProperty.RegisterAttached(
            "EnableBrightness",
            typeof(bool),
            typeof(ActiveAreaHighlight),
            new PropertyMetadata(false, OnEnableBrightnessChanged));

    public static bool GetEnableBrightness(DependencyObject obj) =>
        (bool)obj.GetValue(EnableBrightnessProperty);

    public static void SetEnableBrightness(DependencyObject obj, bool value) =>
        obj.SetValue(EnableBrightnessProperty, value);

    // ── Initialization handlers ──────────────────────────────────

    private static void OnEnableElevationChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not true)
            return;

        // Ensure the element has a mutable DropShadowEffect at resting depth
        if (fe.Effect is not DropShadowEffect)
        {
            fe.Effect = CreateRestingShadow();
        }
    }

    private static void OnEnableBrightnessChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not true)
            return;

        // Ensure the element has a mutable background brush we can animate
        if (fe is Control ctrl)
        {
            if (ctrl.Background is not SolidColorBrush || ctrl.Background.IsFrozen)
                ctrl.Background = new SolidColorBrush(Colors.Transparent);
        }
        else if (fe is Panel panel)
        {
            if (panel.Background is not SolidColorBrush || panel.Background.IsFrozen)
                panel.Background = new SolidColorBrush(Colors.Transparent);
        }
    }

    // ── Core handler ─────────────────────────────────────────────

    private static void OnIsActiveChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe)
            return;

        var active = (bool)e.NewValue;

        if (GetEnableElevation(fe))
            ApplyElevation(fe, active);

        if (GetEnableBrightness(fe))
            ApplyBrightness(fe, active);
    }

    // ── Elevation state ──────────────────────────────────────────

    private static void ApplyElevation(
        FrameworkElement fe, bool active)
    {
        if (fe.Effect is not DropShadowEffect shadow || shadow.IsFrozen)
        {
            shadow = CreateRestingShadow();
            fe.Effect = shadow;
        }

        shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, null);
        shadow.BeginAnimation(DropShadowEffect.ShadowDepthProperty, null);
        shadow.BeginAnimation(DropShadowEffect.OpacityProperty, null);
        shadow.BlurRadius = active ? ActiveBlur : RestingBlur;
        shadow.ShadowDepth = active ? ActiveDepth : RestingDepth;
        shadow.Opacity = active ? ActiveOpacity : RestingOpacity;
    }

    // ── Brightness state ─────────────────────────────────────────

    private static void ApplyBrightness(
        FrameworkElement fe, bool active)
    {
        var brush = fe switch
        {
            Control ctrl => ctrl.Background as SolidColorBrush,
            Panel panel => panel.Background as SolidColorBrush,
            _ => null
        };

        if (brush is null || brush.IsFrozen)
        {
            brush = new SolidColorBrush(Colors.Transparent);
            if (fe is Control c)
                c.Background = brush;
            else if (fe is Panel p)
                p.Background = brush;
        }

        brush.BeginAnimation(SolidColorBrush.ColorProperty, null);
        brush.Color = active ? ActiveTint : Colors.Transparent;
    }

    // ── Factory ──────────────────────────────────────────────────

    private static DropShadowEffect CreateRestingShadow()
    {
        return new DropShadowEffect
        {
            BlurRadius = RestingBlur,
            ShadowDepth = RestingDepth,
            Direction = 270,
            Opacity = RestingOpacity,
            Color = Colors.Black
        };
    }

}
