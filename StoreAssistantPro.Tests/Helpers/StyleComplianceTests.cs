using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace StoreAssistantPro.Tests.Helpers;

/// <summary>
/// Static XAML scanner that enforces UI_RULES.md at build / CI time.
/// <para>
/// Scans every <c>Modules\**\*.xaml</c> view file and reports lines
/// that contain inline colors, hardcoded margins/padding, or direct
/// font assignments that should use DesignSystem tokens instead.
/// </para>
/// <para>
/// Style definition files (<c>Core\Styles\*.xaml</c>) are excluded
/// because they legitimately define the token values.
/// </para>
/// <para>
/// <b>Baseline thresholds:</b> existing legacy violations are capped at
/// a known count.  Fixing a violation lowers the threshold.  Adding a
/// new violation raises the count above the threshold and <b>fails</b>
/// the test.  This allows incremental clean-up without blocking CI.
/// </para>
/// </summary>
public partial class StyleComplianceTests
{
    // ── Baselines (lower these as you fix violations) ─────────────

    /// <summary>Current number of hardcoded Margin/Padding in views.</summary>
    private const int MarginBaseline = 0;

    /// <summary>Current number of hardcoded FontSize/FontFamily in views.</summary>
    private const int FontBaseline = 0;

    /// <summary>Input controls missing AutomationProperties.Name. Lower as you add labels.</summary>
    private const int AccessibilityBaseline = 0;

    private readonly ITestOutputHelper _output;

    public StyleComplianceTests(ITestOutputHelper output) => _output = output;
    // ── Resolve workspace root (walks up from bin/ to solution dir) ───

    private static readonly string SolutionRoot = FindSolutionRoot();

    private static string FindSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0
                || Directory.GetFiles(dir, "*.slnx").Length > 0)
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException(
            "Could not find solution root from " + AppContext.BaseDirectory);
    }

    /// <summary>
    /// Returns all view XAML files under <c>Modules\</c>.
    /// Style dictionaries in <c>Core\Styles\</c> are excluded —
    /// they legitimately define token values.
    /// </summary>
    private static IEnumerable<string> GetViewXamlFiles() =>
        Directory.EnumerateFiles(
                Path.Combine(SolutionRoot, "Modules"), "*.xaml",
                SearchOption.AllDirectories)
            .OrderBy(f => f);

    // ═══════════════════════════════════════════════════════════════════
    //  Reusable scanners
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Scans every view XAML file line-by-line. Skips XML comments.
    /// </summary>
    private static List<string> ScanViews(Regex pattern, string rule)
    {
        var violations = new List<string>();
        foreach (var file in GetViewXamlFiles())
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith("<!--"))
                    continue;
                if (pattern.IsMatch(lines[i]))
                    violations.Add(FormatViolation(file, i + 1, lines[i], rule));
            }
        }
        return violations;
    }

    /// <summary>
    /// Scans every view XAML file line-by-line. Skips XML comments
    /// and lines inside <c>&lt;Style&gt;</c> / <c>&lt;ControlTemplate&gt;</c>
    /// blocks (which legitimately define token values).
    /// </summary>
    private static List<string> ScanViewsSkipStyleBlocks(Regex pattern, string rule)
    {
        var violations = new List<string>();
        foreach (var file in GetViewXamlFiles())
        {
            var lines = File.ReadAllLines(file);
            var inStyleBlock = false;
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("<Style") || trimmed.StartsWith("<ControlTemplate"))
                    inStyleBlock = true;
                if (trimmed.StartsWith("</Style>") || trimmed.StartsWith("</ControlTemplate>"))
                    inStyleBlock = false;
                if (inStyleBlock || trimmed.StartsWith("<!--"))
                    continue;
                if (pattern.IsMatch(lines[i]))
                    violations.Add(FormatViolation(file, i + 1, lines[i], rule));
            }
        }
        return violations;
    }

    private void AssertZero(List<string> violations, string description)
    {
        foreach (var v in violations) _output.WriteLine(v);
        Assert.True(violations.Count == 0,
            $"Found {violations.Count} {description}:\n" + string.Join("\n", violations));
    }

    private void AssertAtBaseline(List<string> violations, int baseline, string description)
    {
        foreach (var v in violations) _output.WriteLine(v);
        Assert.True(violations.Count <= baseline,
            $"{description} count ({violations.Count}) exceeds baseline ({baseline}). "
            + $"Fix violations or lower baseline when cleaning up.\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  1. INLINE COLORS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Detects inline hex colors like <c>Foreground="#616161"</c>.</summary>
    [Fact]
    public void Views_ShouldNot_ContainInlineHexColors() =>
        AssertZero(
            ScanViews(InlineHexColorRegex(),
                "Inline hex color — use {StaticResource Fluent…} token"),
            "inline color(s)");

    // ═══════════════════════════════════════════════════════════════════
    //  2. HARDCODED MARGINS / PADDING
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Detects <c>Margin="0,0,0,12"</c> etc. Allows <c>Margin="0"</c>
    /// and values inside Style/ControlTemplate blocks.
    /// </summary>
    [Fact]
    public void Views_ShouldNot_ContainHardcodedMargins() =>
        AssertAtBaseline(
            ScanViewsSkipStyleBlocks(HardcodedThicknessRegex(),
                "Hardcoded Margin/Padding — use {StaticResource …} token"),
            MarginBaseline, "Hardcoded margin/padding");

    // ═══════════════════════════════════════════════════════════════════
    //  3. DIRECT FONT ASSIGNMENTS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Detects <c>FontSize="13"</c> or <c>FontFamily="Segoe UI"</c>.</summary>
    [Fact]
    public void Views_ShouldNot_ContainDirectFontAssignments() =>
        AssertAtBaseline(
            ScanViewsSkipStyleBlocks(DirectFontRegex(),
                "Hardcoded FontSize/FontFamily — use {StaticResource FontSize…} token"),
            FontBaseline, "Direct font assignment");

    // ═══════════════════════════════════════════════════════════════════
    //  4. DYNAMIC RESOURCE BAN
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>MASTER_RULES §2: Use StaticResource, not DynamicResource.</summary>
    [Fact]
    public void Views_ShouldNot_ContainDynamicResource() =>
        AssertZero(
            ScanViews(DynamicResourceRegex(),
                "DynamicResource — use {StaticResource …} instead"),
            "DynamicResource usage(s)");

    // ═══════════════════════════════════════════════════════════════════
    //  5. INLINE CORNER RADIUS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>MASTER_RULES §2: CornerRadius must use FluentCorner* tokens.</summary>
    [Fact]
    public void Views_ShouldNot_ContainHardcodedCornerRadius() =>
        AssertZero(
            ScanViewsSkipStyleBlocks(HardcodedCornerRadiusRegex(),
                "Hardcoded CornerRadius — use {StaticResource FluentCorner…} token"),
            "hardcoded CornerRadius value(s)");

    // ═══════════════════════════════════════════════════════════════════
    //  6. WINDOW SIZING IN XAML
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES §2: Never set Width, Height, ResizeMode, or
    /// WindowStartupLocation in XAML. Scans Window root elements only.
    /// </summary>
    [Fact]
    public void Windows_ShouldNot_SetSizingInXaml()
    {
        var pattern = WindowSizingRegex();
        var violations = new List<string>();

        foreach (var file in GetViewXamlFiles())
        {
            if (!Path.GetFileName(file).EndsWith("Window.xaml", StringComparison.OrdinalIgnoreCase))
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < Math.Min(lines.Length, 20); i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.Contains("d:Design", StringComparison.Ordinal))
                    continue;
                if (pattern.IsMatch(lines[i]))
                    violations.Add(FormatViolation(file, i + 1, lines[i],
                        "Window sizing in XAML — use WindowSizingService or DialogWidth/DialogHeight"));
                if (trimmed.EndsWith('>') && !trimmed.StartsWith('<'))
                    break;
            }
        }

        AssertZero(violations, "window sizing attribute(s)");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  7. FULL-BODY SCROLL VIEWER
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES §2: "ScrollViewer must never wrap an entire Window,
    /// Dialog, or UserControl root — only around data-driven content."
    /// Detects a <c>&lt;ScrollViewer&gt;</c> as the first child element
    /// of the root container.
    /// </summary>
    [Fact]
    public void Views_ShouldNot_HaveFullBodyScrollViewer()
    {
        var violations = new List<string>();

        foreach (var file in GetViewXamlFiles())
        {
            var lines = File.ReadAllLines(file);
            var pastRoot = false;
            var depth = 0;

            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();

                // Skip until we're past the root element opening
                if (!pastRoot)
                {
                    if (trimmed.StartsWith("<Grid") || trimmed.StartsWith("<DockPanel") ||
                        trimmed.StartsWith("<StackPanel"))
                        pastRoot = true;
                    continue;
                }

                // Skip RowDefinitions, ColumnDefinitions, comments
                if (trimmed.StartsWith("<Grid.") || trimmed.StartsWith("</Grid.") ||
                    trimmed.StartsWith("<RowDef") || trimmed.StartsWith("<ColumnDef") ||
                    trimmed.StartsWith("<!--") || string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // First real child element after root
                if (trimmed.StartsWith("<ScrollViewer") && depth == 0 &&
                    !trimmed.Contains("Grid.Row", StringComparison.Ordinal))
                {
                    violations.Add(FormatViolation(file, i + 1, lines[i],
                        "Full-body ScrollViewer — only wrap data-driven content, not the entire view"));
                }
                break; // only check the first real child
            }
        }

        AssertZero(violations, "full-body ScrollViewer(s)");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  8. DATAGRID STAR COLUMN
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES §2: "At least one DataGrid column uses Width='*'."
    /// Without a star column, the DataGrid may overflow horizontally
    /// or leave unused space.
    /// </summary>
    [Fact]
    public void DataGrids_MustHave_AtLeastOneStarColumn()
    {
        var violations = new List<string>();

        foreach (var file in GetViewXamlFiles())
        {
            var lines = File.ReadAllLines(file);
            var inDataGrid = false;
            var hasStarColumn = false;
            var dataGridStartLine = 0;

            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();

                // Match <DataGrid but not <DataGrid. <DataGridTextColumn etc.
                if (!inDataGrid &&
                    trimmed.StartsWith("<DataGrid", StringComparison.Ordinal) &&
                    !trimmed.StartsWith("<DataGrid.", StringComparison.Ordinal) &&
                    !trimmed.StartsWith("<DataGridText", StringComparison.Ordinal) &&
                    !trimmed.StartsWith("<DataGridTemplate", StringComparison.Ordinal) &&
                    !trimmed.StartsWith("<DataGridCheck", StringComparison.Ordinal) &&
                    !trimmed.StartsWith("<DataGridCombo", StringComparison.Ordinal) &&
                    !trimmed.StartsWith("<DataGridHyperlink", StringComparison.Ordinal))
                {
                    inDataGrid = true;
                    hasStarColumn = false;
                    dataGridStartLine = i + 1;
                }

                if (inDataGrid && trimmed.Contains("Width=\"*\"", StringComparison.Ordinal))
                    hasStarColumn = true;

                if (inDataGrid && trimmed.StartsWith("</DataGrid>", StringComparison.Ordinal))
                {
                    if (!hasStarColumn)
                    {
                        violations.Add(FormatViolation(file, dataGridStartLine, lines[dataGridStartLine - 1],
                            "DataGrid missing Width=\"*\" column — one column must fill remaining space"));
                    }
                    inDataGrid = false;
                }
            }
        }

        AssertZero(violations, "DataGrid(s) missing star column");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  9. RESOURCE KEY UNIQUENESS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ensures no style dictionary file contains duplicate <c>x:Key</c>
    /// definitions. Duplicate keys within a single ResourceDictionary
    /// cause silent overrides — the last definition wins.
    /// <para>
    /// Cross-file duplicates between <c>DesignSystem.xaml</c> and
    /// <c>DensityCompact.xaml</c> are intentional (density overrides)
    /// and excluded.
    /// </para>
    /// </summary>
    [Fact]
    public void StyleDictionaries_ShouldNot_HaveDuplicateKeysInSameFile()
    {
        var keyPattern = ResourceKeyRegex();
        var violations = new List<string>();
        var styleDir = Path.Combine(SolutionRoot, "Core", "Styles");

        foreach (var file in Directory.EnumerateFiles(styleDir, "*.xaml"))
        {
            var keys = new Dictionary<string, int>();
            var lines = File.ReadAllLines(file);

            for (var i = 0; i < lines.Length; i++)
            {
                foreach (System.Text.RegularExpressions.Match match in keyPattern.Matches(lines[i]))
                {
                    var key = match.Groups[1].Value;
                    if (keys.TryGetValue(key, out var firstLine))
                    {
                        violations.Add(FormatViolation(file, i + 1, lines[i],
                            $"Duplicate x:Key=\"{key}\" (first defined on line {firstLine})"));
                    }
                    else
                    {
                        keys[key] = i + 1;
                    }
                }
            }
        }

        AssertZero(violations, "duplicate resource key(s)");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  10. HARDCODED ELEMENT SIZES
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Detects hardcoded <c>Width="8"</c> or <c>Height="10"</c> on
    /// non-layout elements. Element sizes must use design-system
    /// tokens (<c>{StaticResource …}</c>).
    /// <para>
    /// Excludes: <c>RowDefinition</c>, <c>ColumnDefinition</c>,
    /// <c>MaxWidth</c>, <c>MinWidth</c>, <c>MaxHeight</c>, <c>MinHeight</c>,
    /// <c>BorderThickness</c>, binding expressions, design-time attributes,
    /// and Style/ControlTemplate blocks.
    /// </para>
    /// </summary>
    [Fact]
    public void Views_ShouldNot_ContainHardcodedElementSizes()
    {
        var pattern = HardcodedElementSizeRegex();
        var violations = new List<string>();

        foreach (var file in GetViewXamlFiles())
        {
            var lines = File.ReadAllLines(file);
            var inStyleBlock = false;

            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();

                if (trimmed.StartsWith("<Style") || trimmed.StartsWith("<ControlTemplate"))
                    inStyleBlock = true;
                if (trimmed.StartsWith("</Style>") || trimmed.StartsWith("</ControlTemplate>"))
                    inStyleBlock = false;
                if (inStyleBlock || trimmed.StartsWith("<!--"))
                    continue;

                // Skip layout definitions — they legitimately use raw numbers
                if (trimmed.StartsWith("<RowDef") || trimmed.StartsWith("<ColumnDef"))
                    continue;

                // Skip Min/Max constraints — often need raw values
                if (trimmed.Contains("MaxWidth", StringComparison.Ordinal) ||
                    trimmed.Contains("MinWidth", StringComparison.Ordinal) ||
                    trimmed.Contains("MaxHeight", StringComparison.Ordinal) ||
                    trimmed.Contains("MinHeight", StringComparison.Ordinal))
                    continue;

                // Skip design-time
                if (trimmed.Contains("d:Design", StringComparison.Ordinal))
                    continue;

                if (pattern.IsMatch(lines[i]))
                {
                    violations.Add(FormatViolation(file, i + 1, lines[i],
                        "Hardcoded Width/Height — use {StaticResource …} token"));
                }
            }
        }

        AssertZero(violations, "hardcoded element size(s)");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  11. HARDCODED FORMAT STRINGS IN BINDINGS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// MASTER_RULES §6: "Formatting: IRegionalSettingsService.FormatCurrency/
    /// Date/Time — never hardcode format strings." In XAML bindings, use
    /// culture-aware standard format specifiers (<c>C</c>, <c>d</c>, <c>g</c>,
    /// <c>t</c>) instead of hardcoded patterns or currency symbols.
    /// <para>
    /// Detects: <c>StringFormat=₹…</c>, <c>StringFormat=dd-…</c>,
    /// <c>StringFormat=HH:mm</c>, etc. in view XAML files.
    /// Allows: <c>StringFormat={}{0:C}</c>, <c>StringFormat=g</c>,
    /// <c>StringFormat={}{0:N2}</c> (numeric with no symbol).
    /// </para>
    /// </summary>
    [Fact]
    public void Views_ShouldNot_ContainHardcodedFormatStrings()
    {
        var pattern = HardcodedFormatStringRegex();
        var violations = new List<string>();

        foreach (var file in GetViewXamlFiles())
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("<!--"))
                    continue;

                if (pattern.IsMatch(lines[i]))
                    violations.Add(FormatViolation(file, i + 1, lines[i],
                        "Hardcoded format string — use culture-aware format (C, d, g, t)"));
            }
        }

        AssertZero(violations, "hardcoded format string(s)");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  12. VIEWS MUST NOT REFERENCE UNDEFINED RESOURCE KEYS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Scans all view XAML files for <c>{StaticResource KeyName}</c>
    /// references and verifies that <c>KeyName</c> is defined in at
    /// least one style dictionary (<c>Core/Styles/</c> or <c>UI/</c>).
    /// <para>
    /// Undefined references fail silently at runtime, causing missing
    /// colors, margins, or styles that are invisible during development
    /// but break the UI at runtime.
    /// </para>
    /// </summary>
    [Fact]
    public void Views_StaticResources_MustReferenceDefinedKeys()
    {
        // Build the set of all defined resource keys
        var definedKeys = new HashSet<string>(StringComparer.Ordinal);
        var styleDirs = new[]
        {
            Path.Combine(SolutionRoot, "Core", "Styles"),
            Path.Combine(SolutionRoot, "UI", "Styles"),
            Path.Combine(SolutionRoot, "UI", "Themes")
        };

        var keyPattern = ResourceKeyRegex();
        foreach (var dir in styleDirs)
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.EnumerateFiles(dir, "*.xaml"))
            {
                foreach (var line in File.ReadAllLines(file))
                {
                    foreach (System.Text.RegularExpressions.Match match in keyPattern.Matches(line))
                        definedKeys.Add(match.Groups[1].Value);
                }
            }
        }

        // Also capture keys defined inline in App.xaml
        var appXaml = Path.Combine(SolutionRoot, "App.xaml");
        if (File.Exists(appXaml))
        {
            foreach (var line in File.ReadAllLines(appXaml))
            {
                foreach (System.Text.RegularExpressions.Match match in keyPattern.Matches(line))
                    definedKeys.Add(match.Groups[1].Value);
            }
        }

        // Scan views for StaticResource references
        var refPattern = StaticResourceRefRegex();
        var violations = new List<string>();

        // Known WPF system resources that don't need x:Key definitions
        var systemKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "BoolToVisibility", "BooleanToVisibilityConverter",
            "RoleColorConverter", "InverseBoolConverter"
        };

        foreach (var file in GetViewXamlFiles())
        {
            var lines = File.ReadAllLines(file);

            // Collect keys defined locally in this view (Window.Resources, etc.)
            var localKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var line in lines)
            {
                foreach (System.Text.RegularExpressions.Match match in keyPattern.Matches(line))
                    localKeys.Add(match.Groups[1].Value);
            }

            for (var i = 0; i < lines.Length; i++)
            {
                foreach (System.Text.RegularExpressions.Match match in refPattern.Matches(lines[i]))
                {
                    var key = match.Groups[1].Value;
                    if (!definedKeys.Contains(key) && !systemKeys.Contains(key) && !localKeys.Contains(key))
                    {
                        violations.Add(FormatViolation(file, i + 1, lines[i],
                            $"StaticResource '{key}' not found in any style dictionary"));
                    }
                }
            }
        }

        // Only report if many — a few may be from App.xaml inline styles
        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} StaticResource reference(s) to undefined keys:\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Regex patterns (source-generated for performance)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Matches brush properties with inline hex values:
    /// <c>Foreground="#…"</c>, <c>Background="#…"</c>, etc.
    /// Does NOT match <c>Background="Transparent"</c> or
    /// <c>Background="{StaticResource …}"</c>.
    /// </summary>
    [GeneratedRegex(
        @"(?:Foreground|Background|BorderBrush|Fill|Stroke|Color)\s*=\s*""#[0-9A-Fa-f]+""",
        RegexOptions.Compiled)]
    private static partial Regex InlineHexColorRegex();

    /// <summary>
    /// Matches hardcoded thickness values like <c>Margin="8,4"</c> or
    /// <c>Padding="0,0,0,12"</c>.
    /// Excludes: <c>Margin="0"</c>, <c>Margin="{…}"</c>.
    /// </summary>
    [GeneratedRegex(
        @"(?:Margin|Padding)\s*=\s*""(?!0"")(?!\{)[0-9][0-9,. ]*""",
        RegexOptions.Compiled)]
    private static partial Regex HardcodedThicknessRegex();

    /// <summary>
    /// Matches literal font assignments like <c>FontSize="13"</c> or
    /// <c>FontFamily="Segoe UI"</c>.
    /// Excludes: <c>FontSize="{StaticResource …}"</c>.
    /// </summary>
    [GeneratedRegex(
        @"(?:FontSize|FontFamily)\s*=\s*""(?!\{)[^""]+""",
        RegexOptions.Compiled)]
    private static partial Regex DirectFontRegex();

    /// <summary>
    /// Matches <c>{DynamicResource …}</c> bindings.
    /// </summary>
    [GeneratedRegex(
        @"\{DynamicResource\s",
        RegexOptions.Compiled)]
    private static partial Regex DynamicResourceRegex();

    /// <summary>
    /// Matches <c>CornerRadius="digits"</c>.
    /// Excludes <c>CornerRadius="{StaticResource …}"</c>.
    /// </summary>
    [GeneratedRegex(
        @"CornerRadius\s*=\s*""(?!\{)[0-9][0-9,. ]*""",
        RegexOptions.Compiled)]
    private static partial Regex HardcodedCornerRadiusRegex();

    /// <summary>
    /// Matches window-level sizing attributes:
    /// <c>Width="…"</c>, <c>Height="…"</c> (non-binding),
    /// <c>ResizeMode="…"</c>, <c>WindowStartupLocation="…"</c>.
    /// Excludes binding expressions.
    /// </summary>
    [GeneratedRegex(
        @"\b(?:(?:Width|Height)\s*=\s*""(?!\{)\d|ResizeMode\s*=|WindowStartupLocation\s*=)",
        RegexOptions.Compiled)]
    private static partial Regex WindowSizingRegex();

    /// <summary>
    /// Extracts <c>x:Key="…"</c> values from XAML resource definitions.
    /// </summary>
    [GeneratedRegex(
        @"x:Key=""([^""]+)""",
        RegexOptions.Compiled)]
    private static partial Regex ResourceKeyRegex();

    /// <summary>
    /// Matches hardcoded element sizes: <c>Width="8"</c>, <c>Height="400"</c>.
    /// Excludes: bindings (<c>{…}</c>), <c>Auto</c>, <c>*</c> (star sizing),
    /// and <c>NaN</c>. Only matches standalone Width/Height attributes.
    /// </summary>
    [GeneratedRegex(
        @"(?<!\w)(?:Width|Height)\s*=\s*""(?!\{)(?!Auto)(?!\*)(?!NaN)\d+(?:\.\d+)?""",
        RegexOptions.Compiled)]
    private static partial Regex HardcodedElementSizeRegex();

    /// <summary>
    /// Extracts resource key names from <c>{StaticResource KeyName}</c> references.
    /// </summary>
    [GeneratedRegex(
        @"\{StaticResource\s+(\w+)\}",
        RegexOptions.Compiled)]
    private static partial Regex StaticResourceRefRegex();

    /// <summary>Matches opening tags for input controls: TextBox, PasswordBox, ComboBox, DatePicker.</summary>
    [GeneratedRegex(
        @"<(TextBox|PasswordBox|ComboBox|DatePicker)\b",
        RegexOptions.Compiled)]
    private static partial Regex InputControlRegex();

    /// <summary>
    /// Matches hardcoded format strings in <c>StringFormat=</c> attributes:
    /// currency symbols (₹, $, €) or date/time patterns (dd, MM, yy, HH, hh, mm, ss, tt).
    /// Does NOT match standard format specifiers like <c>C</c>, <c>d</c>, <c>g</c>, <c>t</c>,
    /// <c>N2</c>, <c>G</c>, etc.
    /// </summary>
    [GeneratedRegex(
        @"StringFormat=[^""]*(?:₹|\$|€|dd|MM|yyyy|yy|HH|hh|mm(?!})|ss|tt)",
        RegexOptions.Compiled)]
    private static partial Regex HardcodedFormatStringRegex();

    // ═══════════════════════════════════════════════════════════════════
    //  13. UI → CORE KEY OVERLAP BUDGET
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Known overlapping keys between <c>UI/</c> and <c>Core/Styles/</c>.
    /// Core wins (merged later). This budget prevents accidental growth.
    /// </summary>
    private const int UiCoreOverlapBudget = 2; // CardStyle, AppBackgroundBrush

    /// <summary>
    /// Caps the number of resource keys defined in both <c>UI/</c>
    /// and <c>Core/Styles/</c>. Overlaps are intentional (Core overrides
    /// UI), but new ones should not be added without a clear reason.
    /// </summary>
    [Fact]
    public void StyleDictionaries_UiCorOverlap_MustNotExceedBudget()
    {
        var coreDir = Path.Combine(SolutionRoot, "Core", "Styles");
        var keyPattern = ResourceKeyRegex();

        var coreKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(coreDir, "*.xaml"))
        {
            foreach (Match m in keyPattern.Matches(File.ReadAllText(file)))
                coreKeys.Add(m.Groups[1].Value);
        }

        var overlaps = new List<string>();
        foreach (var dir in new[] { "UI/Styles", "UI/Themes" })
        {
            var uiDir = Path.Combine(SolutionRoot, dir);
            if (!Directory.Exists(uiDir)) continue;

            foreach (var file in Directory.EnumerateFiles(uiDir, "*.xaml"))
            {
                foreach (Match m in keyPattern.Matches(File.ReadAllText(file)))
                {
                    if (coreKeys.Contains(m.Groups[1].Value))
                        overlaps.Add($"  {Path.GetFileName(file)}: {m.Groups[1].Value} (also in Core/Styles)");
                }
            }
        }

        foreach (var v in overlaps)
            _output.WriteLine(v);

        Assert.True(overlaps.Count <= UiCoreOverlapBudget,
            $"UI/Core key overlap count ({overlaps.Count}) exceeds budget ({UiCoreOverlapBudget}). "
            + "Review whether new overlaps are intentional.\n"
            + string.Join("\n", overlaps));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  14. INPUT CONTROLS MUST HAVE AUTOMATION NAMES
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Accessibility: every <c>TextBox</c>, <c>PasswordBox</c>,
    /// <c>ComboBox</c>, and <c>DatePicker</c> should have
    /// <c>AutomationProperties.Name</c>
    /// for screen readers. Uses a baseline to allow incremental cleanup.
    /// </summary>
    [Fact]
    public void InputControls_ShouldHave_AutomationName()
    {
        var violations = new List<string>();

        foreach (var file in GetViewXamlFiles())
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (!InputControlRegex().IsMatch(lines[i]))
                    continue;

                // Collect the element block (up to closing > or />)
                var block = lines[i];
                for (var j = i + 1; j < Math.Min(i + 4, lines.Length); j++)
                {
                    block += " " + lines[j];
                    if (lines[j].Contains("/>") || lines[j].Contains(">"))
                        break;
                }

                if (!block.Contains("AutomationProperties", StringComparison.Ordinal))
                {
                    violations.Add(FormatViolation(file, i + 1, lines[i],
                        "Input control missing AutomationProperties.Name"));
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count <= AccessibilityBaseline,
            $"Missing AutomationProperties count ({violations.Count}) exceeds baseline ({AccessibilityBaseline}). "
            + "Add AutomationProperties.Name to new input controls. Lower baseline as you fix existing ones.\n"
            + string.Join("\n", violations));
    }

    // ── Formatting ───────────────────────────────────────────────────

    private static string FormatViolation(
        string file, int line, string content, string rule)
    {
        var relative = Path.GetRelativePath(SolutionRoot, file);
        return $"  {relative}({line}): {rule}\n    {content.Trim()}";
    }
}
