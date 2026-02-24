using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Debug-only attached behavior that validates UI style compliance
/// on every <see cref="Window"/> at load time.
/// <para>
/// <b>Detects at runtime:</b>
/// <list type="bullet">
///   <item>Inline hex colors on <c>Foreground</c>, <c>Background</c>,
///         <c>BorderBrush</c> (i.e. locally-set <see cref="SolidColorBrush"/>
///         values that are not <c>StaticResource</c> references).</item>
///   <item>Locally-set <c>Margin</c> / <c>Padding</c> with non-zero values
///         that bypass the design-system <c>Thickness</c> tokens.</item>
///   <item>Locally-set <c>FontSize</c> or <c>FontFamily</c> that bypass
///         the typography scale.</item>
/// </list>
/// </para>
/// <para>
/// Warnings appear in the Visual Studio <b>Output → Debug</b> pane.
/// The behavior compiles to a no-op in Release builds.
/// </para>
/// <para>
/// <b>Activation (GlobalStyles.xaml):</b>
/// </para>
/// <code>
/// &lt;Style TargetType="Window"&gt;
///     &lt;Setter Property="h:StyleComplianceDiagnostics.IsEnabled" Value="True"/&gt;
/// &lt;/Style&gt;
/// </code>
/// </summary>
public static class StyleComplianceDiagnostics
{
    // ── Attached property ────────────────────────────────────────────

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(StyleComplianceDiagnostics),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
#if DEBUG
        if (d is Window window && e.NewValue is true)
            window.Loaded += OnWindowLoaded;
#endif
    }

#if DEBUG
    // ── Known design-system brushes (populated lazily) ───────────────

    private static HashSet<Color>? _systemColors;

    /// <summary>
    /// Collects all <see cref="SolidColorBrush"/> colors defined in
    /// <c>DesignSystem.xaml</c> so we can distinguish them from
    /// locally-constructed brushes at runtime.
    /// </summary>
    private static HashSet<Color> GetSystemColors()
    {
        if (_systemColors is not null)
            return _systemColors;

        _systemColors = [];

        if (Application.Current?.Resources is not { } res)
            return _systemColors;

        foreach (var key in res.Keys)
        {
            if (res[key] is SolidColorBrush brush)
                _systemColors.Add(brush.Color);
        }

        // Walk merged dictionaries (DesignSystem, FluentTheme, etc.)
        foreach (var merged in res.MergedDictionaries)
        {
            foreach (var key in merged.Keys)
            {
                if (merged[key] is SolidColorBrush brush)
                    _systemColors.Add(brush.Color);
            }
        }

        return _systemColors;
    }

    // ── Window-loaded handler ────────────────────────────────────────

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Window window)
            return;

        var windowName = window.GetType().Name;
        var count = 0;

        WalkVisualTree(window, windowName, ref count, depth: 0);

        if (count > 0)
            Warn(windowName, $"Style compliance scan complete — {count} warning(s).");
    }

    // ── Visual tree walk ─────────────────────────────────────────────

    private static void WalkVisualTree(
        DependencyObject parent, string windowName, ref int count, int depth)
    {
        if (depth > 30)
            return;

        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is FrameworkElement fe)
            {
                CheckInlineColor(fe, windowName, ref count);
                CheckHardcodedThickness(fe, windowName, ref count);
                CheckDirectFont(fe, windowName, ref count);
            }

            WalkVisualTree(child, windowName, ref count, depth + 1);
        }
    }

    // ── Check 1: Inline colors ───────────────────────────────────────

    private static readonly DependencyProperty[] _brushProperties =
    [
        Control.ForegroundProperty,
        Control.BackgroundProperty,
        Control.BorderBrushProperty,
        Panel.BackgroundProperty,
        Border.BackgroundProperty,
        Border.BorderBrushProperty,
        TextBlock.ForegroundProperty,
    ];

    private static void CheckInlineColor(
        FrameworkElement fe, string windowName, ref int count)
    {
        var systemColors = GetSystemColors();

        foreach (var prop in _brushProperties)
        {
            // Only report values set locally (not from styles, templates, or inheritance)
            var source = DependencyPropertyHelper.GetValueSource(fe, prop);
            if (source.BaseValueSource is not BaseValueSource.Local)
                continue;

            if (fe.GetValue(prop) is not SolidColorBrush brush)
                continue;

            // Transparent and known system colors are allowed
            if (brush.Color.A == 0)
                continue;

            if (systemColors.Contains(brush.Color))
                continue;

            WarnViolation(windowName, fe, prop.Name,
                $"inline color #{brush.Color.ToString()[3..]}", ref count);
        }
    }

    // ── Check 2: Hardcoded margins / padding ─────────────────────────

    private static readonly DependencyProperty[] _thicknessProperties =
    [
        FrameworkElement.MarginProperty,
        Control.PaddingProperty,
        Border.PaddingProperty,
    ];

    private static void CheckHardcodedThickness(
        FrameworkElement fe, string windowName, ref int count)
    {
        foreach (var prop in _thicknessProperties)
        {
            var source = DependencyPropertyHelper.GetValueSource(fe, prop);
            if (source.BaseValueSource is not BaseValueSource.Local)
                continue;

            var value = (Thickness)fe.GetValue(prop);

            // Margin="0" is an explicit reset — always allowed
            if (value is { Left: 0, Top: 0, Right: 0, Bottom: 0 })
                continue;

            WarnViolation(windowName, fe, prop.Name,
                $"hardcoded {FormatThickness(value)}", ref count);
        }
    }

    // ── Check 3: Direct font assignments ─────────────────────────────

    private static void CheckDirectFont(
        FrameworkElement fe, string windowName, ref int count)
    {
        // FontSize
        var fsSource = DependencyPropertyHelper.GetValueSource(fe, TextBlock.FontSizeProperty);
        if (fsSource.BaseValueSource is BaseValueSource.Local)
        {
            // Controls inside style templates and DataTemplate re-apply
            // font size locally — only flag top-level authored elements.
            if (!IsInsideTemplate(fe))
            {
                var size = (double)fe.GetValue(TextBlock.FontSizeProperty);
                WarnViolation(windowName, fe, "FontSize",
                    $"hardcoded {size}", ref count);
            }
        }

        // FontFamily
        var ffSource = DependencyPropertyHelper.GetValueSource(fe, TextBlock.FontFamilyProperty);
        if (ffSource.BaseValueSource is BaseValueSource.Local)
        {
            if (!IsInsideTemplate(fe))
            {
                var family = fe.GetValue(TextBlock.FontFamilyProperty) as FontFamily;
                WarnViolation(windowName, fe, "FontFamily",
                    $"hardcoded \"{family?.Source}\"", ref count);
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> when the element was created by a
    /// <c>ControlTemplate</c> or <c>DataTemplate</c> rather than
    /// authored directly in the view XAML. Template internals
    /// legitimately set local values.
    /// </summary>
    private static bool IsInsideTemplate(FrameworkElement fe) =>
        fe.TemplatedParent is not null;

    private static void WarnViolation(
        string windowName, FrameworkElement fe,
        string property, string detail, ref int count)
    {
        count++;
        var name = string.IsNullOrEmpty(fe.Name) ? fe.GetType().Name : fe.Name;
        Warn(windowName,
            $"[{name}] {property} uses {detail} — use a DesignSystem token instead.");
    }

    private static string FormatThickness(Thickness t) =>
        t.Left == t.Top && t.Top == t.Right && t.Right == t.Bottom
            ? $"Margin/Padding=\"{t.Left}\""
            : $"Margin/Padding=\"{t.Left},{t.Top},{t.Right},{t.Bottom}\"";

    private static void Warn(string windowName, string message)
    {
        var text = $"[StyleCompliance] {windowName}: {message}";
        Debug.WriteLine(text);
        Trace.TraceWarning(text);
    }
#endif
}
