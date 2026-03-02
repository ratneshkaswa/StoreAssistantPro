using System.Text.RegularExpressions;

namespace StoreAssistantPro.Tests.Helpers;

/// <summary>
/// Compliance tests for the <c>EmptyStateOverlay</c> migration.
/// Ensures all DataGrid views use the reusable control instead of
/// inline TextBlock + DataTrigger patterns.
/// </summary>
public partial class EmptyStateOverlayComplianceTests
{
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
        throw new InvalidOperationException("Cannot find solution root.");
    }

    // ══════════════════════════════════════════════════════════════════
    //  EmptyStateOverlay control and template exist
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void EmptyStateOverlay_ControlFileExists()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Controls", "EmptyStateOverlay.cs");
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void EmptyStateOverlay_HasTemplateInGlobalStyles()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml");
        var content = File.ReadAllText(path);

        Assert.Contains("TargetType=\"controls:EmptyStateOverlay\"", content, StringComparison.Ordinal);
        Assert.Contains("ControlTemplate", content, StringComparison.Ordinal);
    }

    // ── Regex ────────────────────────────────────────────────────────

    /// <summary>
    /// Matches the old inline empty-state pattern:
    /// <c>&lt;TextBlock Text="... No ... found"</c> followed by a
    /// style block with a DataTrigger on <c>.Count</c> Value="0".
    /// </summary>
    [GeneratedRegex(
        @"<TextBlock[^>]*Text=""[^""]*No\s+\w+\s+found""[^>]*>.*?<DataTrigger[^>]*Value=""0""",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex InlineEmptyStateRegex();
}
