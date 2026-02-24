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
    private const int MarginBaseline = 99;

    /// <summary>Current number of hardcoded FontSize/FontFamily in views.</summary>
    private const int FontBaseline = 24;

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
    //  1. INLINE COLORS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Detects attributes like <c>Foreground="#616161"</c> or
    /// <c>Background="#FFFFFF"</c> in view XAML.  Colors must come
    /// from <c>{StaticResource Fluent…}</c> tokens.
    /// </summary>
    [Fact]
    public void Views_ShouldNot_ContainInlineHexColors()
    {
        // Matches: Foreground="#xxx", Background="#xxx", BorderBrush="#xxx",
        //          Fill="#xxx", Stroke="#xxx", Color="#xxx"
        //          but NOT inside DesignSystem/FluentTheme/GlobalStyles.
        var pattern = InlineHexColorRegex();
        var violations = new List<string>();

        foreach (var file in GetViewXamlFiles())
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Skip design-time attributes and XML comments
                if (line.TrimStart().StartsWith("<!--"))
                    continue;

                if (pattern.IsMatch(line))
                {
                    violations.Add(FormatViolation(file, i + 1, line,
                        "Inline hex color — use {StaticResource Fluent…} token"));
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} inline color(s) in view files:\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  2. HARDCODED MARGINS / PADDING
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Detects attributes like <c>Margin="0,0,0,12"</c> or
    /// <c>Padding="10,5"</c>.  Spacing must come from design-system
    /// <c>Thickness</c> tokens.
    /// <para>
    /// <b>Allowed:</b> <c>Margin="0"</c> (explicit reset) and values
    /// inside <c>&lt;Style&gt;</c> / <c>&lt;ControlTemplate&gt;</c>
    /// blocks within the same file.
    /// </para>
    /// </summary>
    [Fact]
    public void Views_ShouldNot_ContainHardcodedMargins()
    {
        // Matches: Margin="digits..." or Padding="digits..."
        // but NOT Margin="0" (pure reset) or Margin="{StaticResource …}"
        var pattern = HardcodedThicknessRegex();
        var violations = new List<string>();

        foreach (var file in GetViewXamlFiles())
        {
            var lines = File.ReadAllLines(file);
            var inStyleBlock = false;

            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();

                // Track <Style> and <ControlTemplate> blocks — they are
                // allowed to set hardcoded values (they ARE the styles).
                if (trimmed.StartsWith("<Style") || trimmed.StartsWith("<ControlTemplate"))
                    inStyleBlock = true;
                if (trimmed.StartsWith("</Style>") || trimmed.StartsWith("</ControlTemplate>"))
                    inStyleBlock = false;

                if (inStyleBlock)
                    continue;

                if (trimmed.StartsWith("<!--"))
                    continue;

                if (pattern.IsMatch(lines[i]))
                {
                    violations.Add(FormatViolation(file, i + 1, lines[i],
                        "Hardcoded Margin/Padding — use {StaticResource …} token"));
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count <= MarginBaseline,
            $"Hardcoded margin/padding count ({violations.Count}) exceeds baseline ({MarginBaseline}). "
            + "New code must use {{StaticResource …}} tokens. "
            + $"Fix violations or lower MarginBaseline when cleaning up.\n"
            + string.Join("\n", violations));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  3. DIRECT FONT ASSIGNMENTS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Detects attributes like <c>FontSize="13"</c> or
    /// <c>FontFamily="Segoe UI"</c>.  Typography must come from the
    /// type-scale tokens or named styles.
    /// </summary>
    [Fact]
    public void Views_ShouldNot_ContainDirectFontAssignments()
    {
        var pattern = DirectFontRegex();
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

                if (inStyleBlock)
                    continue;

                if (trimmed.StartsWith("<!--"))
                    continue;

                if (pattern.IsMatch(lines[i]))
                {
                    violations.Add(FormatViolation(file, i + 1, lines[i],
                        "Hardcoded FontSize/FontFamily — use {StaticResource FontSize…} token"));
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.True(violations.Count <= FontBaseline,
            $"Direct font assignment count ({violations.Count}) exceeds baseline ({FontBaseline}). "
            + "New code must use {{StaticResource FontSize…}} tokens. "
            + $"Fix violations or lower FontBaseline when cleaning up.\n"
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

    // ── Formatting ───────────────────────────────────────────────────

    private static string FormatViolation(
        string file, int line, string content, string rule)
    {
        var relative = Path.GetRelativePath(SolutionRoot, file);
        return $"  {relative}({line}): {rule}\n    {content.Trim()}";
    }
}
