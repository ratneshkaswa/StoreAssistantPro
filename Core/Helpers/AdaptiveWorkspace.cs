using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that adapts the main workspace layout based on
/// the operational mode.
/// <para>
/// <b>Billing mode:</b> The workspace border receives a subtle accent
/// glow and the hosting panel's non-workspace rows (menu, toolbar,
/// status bar) animate to compact heights via <see cref="LayoutTransform"/>
/// scale, maximising the billing content area. The workspace padding
/// tightens so the billing panel fills more screen real-estate.
/// </para>
/// <para>
/// <b>Management mode:</b> Standard layout — balanced visual weight
/// across all chrome elements, default padding on the workspace.
/// </para>
/// <para>
/// All transitions are animated using <c>FluentDurationNormal</c>
/// (133 ms) with <c>PanelDimEase</c> for smooth, non-jarring
/// layout shifts. The animations target only <c>Opacity</c>,
/// <c>LayoutTransform.ScaleY</c>, and <c>BorderBrush</c> — no
/// sudden visibility toggles that would cause layout jumps.
/// </para>
///
/// <para><b>Properties:</b></para>
/// <list type="table">
///   <listheader><term>Property</term><description>Role</description></listheader>
///   <item>
///     <term><see cref="IsBillingActiveProperty"/></term>
///     <description>Master driver — bind to <c>FocusLock.IsFocusLocked</c>.
///     All transitions fire when this changes.</description>
///   </item>
///   <item>
///     <term><see cref="CompactChromeProperty"/></term>
///     <description>Set on menu, toolbar, status bar. Animates ScaleY
///     from 1.0 → <see cref="ChromeScaleProperty"/> in billing mode,
///     shrinking the chrome without collapsing it.</description>
///   </item>
///   <item>
///     <term><see cref="WorkspaceGlowProperty"/></term>
///     <description>Set on the workspace <c>ContentControl</c>.
///     Animates a subtle accent border when billing is active.</description>
///   </item>
/// </list>
///
/// <para><b>Usage (MainWindow.xaml):</b></para>
/// <code>
/// &lt;Menu h:AdaptiveWorkspace.CompactChrome="True"
///       h:AdaptiveWorkspace.IsBillingActive="{Binding FocusLock.IsFocusLocked}" .../&gt;
///
/// &lt;controls:ResponsiveContentControl
///       h:AdaptiveWorkspace.WorkspaceGlow="True"
///       h:AdaptiveWorkspace.IsBillingActive="{Binding FocusLock.IsFocusLocked}" .../&gt;
/// </code>
/// </summary>
public static class AdaptiveWorkspace
{
    private const double DefaultChromeScale = 0.85;

    // ── IsBillingActive DP (master driver) ─────────────────────────

    public static readonly DependencyProperty IsBillingActiveProperty =
        DependencyProperty.RegisterAttached(
            "IsBillingActive",
            typeof(bool),
            typeof(AdaptiveWorkspace),
            new PropertyMetadata(false, OnIsBillingActiveChanged));

    public static bool GetIsBillingActive(DependencyObject obj) =>
        (bool)obj.GetValue(IsBillingActiveProperty);

    public static void SetIsBillingActive(DependencyObject obj, bool value) =>
        obj.SetValue(IsBillingActiveProperty, value);

    // ── CompactChrome DP ───────────────────────────────────────────

    /// <summary>
    /// Set on chrome elements (menu, toolbar, status bar) to
    /// smoothly scale them down in billing mode.
    /// </summary>
    public static readonly DependencyProperty CompactChromeProperty =
        DependencyProperty.RegisterAttached(
            "CompactChrome",
            typeof(bool),
            typeof(AdaptiveWorkspace),
            new PropertyMetadata(false));

    public static bool GetCompactChrome(DependencyObject obj) =>
        (bool)obj.GetValue(CompactChromeProperty);

    public static void SetCompactChrome(DependencyObject obj, bool value) =>
        obj.SetValue(CompactChromeProperty, value);

    // ── ChromeScale DP ─────────────────────────────────────────────

    /// <summary>
    /// Target ScaleY for chrome elements in billing mode (default 0.85).
    /// </summary>
    public static readonly DependencyProperty ChromeScaleProperty =
        DependencyProperty.RegisterAttached(
            "ChromeScale",
            typeof(double),
            typeof(AdaptiveWorkspace),
            new PropertyMetadata(DefaultChromeScale));

    public static double GetChromeScale(DependencyObject obj) =>
        (double)obj.GetValue(ChromeScaleProperty);

    public static void SetChromeScale(DependencyObject obj, double value) =>
        obj.SetValue(ChromeScaleProperty, value);

    // ── WorkspaceGlow DP ───────────────────────────────────────────

    /// <summary>
    /// Set on the workspace content control to animate a subtle
    /// accent-colored border when billing mode is active.
    /// </summary>
    public static readonly DependencyProperty WorkspaceGlowProperty =
        DependencyProperty.RegisterAttached(
            "WorkspaceGlow",
            typeof(bool),
            typeof(AdaptiveWorkspace),
            new PropertyMetadata(false, OnWorkspaceGlowChanged));

    public static bool GetWorkspaceGlow(DependencyObject obj) =>
        (bool)obj.GetValue(WorkspaceGlowProperty);

    public static void SetWorkspaceGlow(DependencyObject obj, bool value) =>
        obj.SetValue(WorkspaceGlowProperty, value);

    // ── Core handler ───────────────────────────────────────────────

    private static void OnIsBillingActiveChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe)
            return;

        var billing = (bool)e.NewValue;

        if (GetCompactChrome(fe))
            AnimateChromeScale(fe, billing);

        if (GetWorkspaceGlow(fe))
            AnimateGlow(fe, billing);
    }

    // ── WorkspaceGlow initialization ───────────────────────────────

    private static void OnWorkspaceGlowChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not true)
            return;

        // Ensure the element has a border we can animate.
        // For ContentControl, set an initial transparent border.
        if (fe is Control ctrl)
        {
            ctrl.BorderThickness = new Thickness(2);
            ctrl.BorderBrush = new SolidColorBrush(Colors.Transparent);
        }
    }

    // ── Chrome scale animation ─────────────────────────────────────

    private static void AnimateChromeScale(FrameworkElement fe, bool billing)
    {
        var target = billing ? GetChromeScale(fe) : 1.0;
        var duration = ResolveDuration(fe);
        var ease = ResolveEase(fe);

        // Ensure LayoutTransform is a ScaleTransform
        if (fe.LayoutTransform is not ScaleTransform st)
        {
            st = new ScaleTransform(1, 1);
            fe.LayoutTransform = st;
        }

        var anim = new DoubleAnimation(target, duration)
        {
            EasingFunction = ease
        };
        anim.Freeze();
        st.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
    }

    // ── Workspace glow animation ───────────────────────────────────

    private static void AnimateGlow(FrameworkElement fe, bool billing)
    {
        if (fe is not Control ctrl)
            return;

        var duration = ResolveDuration(fe);
        var ease = ResolveEase(fe);

        // Resolve accent color from design system
        var accentColor = Colors.Transparent;
        if (billing)
        {
            if (fe.TryFindResource("FluentAccentDefault") is SolidColorBrush ab)
                accentColor = ab.Color;
            else
                accentColor = Color.FromRgb(0x00, 0x5F, 0xB8);
        }

        // Ensure mutable brush
        if (ctrl.BorderBrush is not SolidColorBrush brush || brush.IsFrozen)
        {
            brush = new SolidColorBrush(Colors.Transparent);
            ctrl.BorderBrush = brush;
        }

        var colorAnim = new ColorAnimation(accentColor, duration)
        {
            EasingFunction = ease
        };
        colorAnim.Freeze();
        brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
    }

    // ── Token resolution ───────────────────────────────────────────

    private static Duration ResolveDuration(FrameworkElement fe)
    {
        if (fe.TryFindResource("FluentDurationNormal") is Duration d && d.HasTimeSpan)
            return d;
        return new Duration(TimeSpan.FromMilliseconds(133));
    }

    private static IEasingFunction? ResolveEase(FrameworkElement fe) =>
        fe.TryFindResource("PanelDimEase") as IEasingFunction;
}
