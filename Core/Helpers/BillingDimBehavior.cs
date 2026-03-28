using System.Windows;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Attached behavior that smoothly dims or highlights a
/// <see cref="FrameworkElement"/> when the billing focus lock
/// activates or deactivates.
/// <para>
/// <b>Dim mode</b> (<see cref="DimOnLockProperty"/> = <c>True</c>):
/// Animates the element's <see cref="UIElement.Opacity"/> from 1.0
/// down to <see cref="DimOpacityProperty"/> when the bound
/// <see cref="IsLockedProperty"/> becomes <c>True</c>, and back to
/// 1.0 when it becomes <c>False</c>.
/// </para>
/// <para>
/// <b>Highlight mode</b> (<see cref="HighlightOnLockProperty"/> = <c>True</c>):
/// Animates the element's <see cref="UIElement.Opacity"/> from
/// <see cref="HighlightFromOpacityProperty"/> up to 1.0 when the lock
/// activates, providing a subtle "pop" that draws the eye to the
/// billing workspace.
/// </para>
/// <para>
/// The behavior applies direct opacity states so shell changes feel
/// immediate and avoid extra composition work.
/// </para>
///
/// <para><b>Usage (MainWindow.xaml):</b></para>
/// <code>
/// &lt;!-- Dim non-billing areas --&gt;
/// &lt;Menu h:BillingDimBehavior.DimOnLock="True"
///       h:BillingDimBehavior.IsLocked="{Binding FocusLock.IsFocusLocked}" .../&gt;
///
/// &lt;!-- Highlight the billing workspace --&gt;
/// &lt;controls:ResponsiveContentControl
///       h:BillingDimBehavior.HighlightOnLock="True"
///       h:BillingDimBehavior.IsLocked="{Binding FocusLock.IsFocusLocked}" .../&gt;
/// </code>
/// </summary>
public static class BillingDimBehavior
{
    // ── Default opacity values ───────────────────────────────────────
    private const double DefaultDimOpacity = 0.45;
    private const double DefaultHighlightFromOpacity = 0.92;

    // ── IsLocked DP (drives all transitions) ─────────────────────────

    /// <summary>
    /// Bind to <c>FocusLock.IsFocusLocked</c>. When this changes, the
    /// dim or highlight animation plays on the target element.
    /// </summary>
    public static readonly DependencyProperty IsLockedProperty =
        DependencyProperty.RegisterAttached(
            "IsLocked",
            typeof(bool),
            typeof(BillingDimBehavior),
            new PropertyMetadata(false, OnIsLockedChanged));

    public static bool GetIsLocked(DependencyObject obj) =>
        (bool)obj.GetValue(IsLockedProperty);

    public static void SetIsLocked(DependencyObject obj, bool value) =>
        obj.SetValue(IsLockedProperty, value);

    // ── DimOnLock DP ─────────────────────────────────────────────────

    /// <summary>
    /// Set to <c>True</c> on elements that should dim when the billing
    /// focus lock activates (menu bar, toolbar, status bar).
    /// </summary>
    public static readonly DependencyProperty DimOnLockProperty =
        DependencyProperty.RegisterAttached(
            "DimOnLock",
            typeof(bool),
            typeof(BillingDimBehavior),
            new PropertyMetadata(false));

    public static bool GetDimOnLock(DependencyObject obj) =>
        (bool)obj.GetValue(DimOnLockProperty);

    public static void SetDimOnLock(DependencyObject obj, bool value) =>
        obj.SetValue(DimOnLockProperty, value);

    // ── HighlightOnLock DP ───────────────────────────────────────────

    /// <summary>
    /// Set to <c>True</c> on the billing workspace element that should
    /// receive a subtle opacity pop when the focus lock activates.
    /// </summary>
    public static readonly DependencyProperty HighlightOnLockProperty =
        DependencyProperty.RegisterAttached(
            "HighlightOnLock",
            typeof(bool),
            typeof(BillingDimBehavior),
            new PropertyMetadata(false));

    public static bool GetHighlightOnLock(DependencyObject obj) =>
        (bool)obj.GetValue(HighlightOnLockProperty);

    public static void SetHighlightOnLock(DependencyObject obj, bool value) =>
        obj.SetValue(HighlightOnLockProperty, value);

    // ── DimOpacity DP ────────────────────────────────────────────────

    /// <summary>
    /// Target opacity for dimmed elements (default 0.45).
    /// </summary>
    public static readonly DependencyProperty DimOpacityProperty =
        DependencyProperty.RegisterAttached(
            "DimOpacity",
            typeof(double),
            typeof(BillingDimBehavior),
            new PropertyMetadata(DefaultDimOpacity));

    public static double GetDimOpacity(DependencyObject obj) =>
        (double)obj.GetValue(DimOpacityProperty);

    public static void SetDimOpacity(DependencyObject obj, double value) =>
        obj.SetValue(DimOpacityProperty, value);

    // ── HighlightFromOpacity DP ──────────────────────────────────────

    /// <summary>
    /// Starting opacity for the highlight pop (default 0.92).
    /// Animates from this value to 1.0 on lock activation.
    /// </summary>
    public static readonly DependencyProperty HighlightFromOpacityProperty =
        DependencyProperty.RegisterAttached(
            "HighlightFromOpacity",
            typeof(double),
            typeof(BillingDimBehavior),
            new PropertyMetadata(DefaultHighlightFromOpacity));

    public static double GetHighlightFromOpacity(DependencyObject obj) =>
        (double)obj.GetValue(HighlightFromOpacityProperty);

    public static void SetHighlightFromOpacity(DependencyObject obj, double value) =>
        obj.SetValue(HighlightFromOpacityProperty, value);

    // ── Core handler ─────────────────────────────────────────────────

    private static void OnIsLockedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe)
            return;

        var locked = (bool)e.NewValue;
        var isDim = GetDimOnLock(fe);
        var isHighlight = GetHighlightOnLock(fe);

        if (!isDim && !isHighlight)
            return;

        if (isDim)
        {
            var target = locked ? GetDimOpacity(fe) : 1.0;
            fe.BeginAnimation(UIElement.OpacityProperty, null);
            fe.Opacity = target;
        }

        if (isHighlight)
        {
            fe.BeginAnimation(UIElement.OpacityProperty, null);
            fe.Opacity = 1.0;
        }
    }
}
