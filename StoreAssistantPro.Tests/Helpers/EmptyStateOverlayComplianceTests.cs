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
    //  Each DataGrid view uses EmptyStateOverlay
    // ══════════════════════════════════════════════════════════════════

    [Theory(Skip = "No DataGrid views exist yet — re-enable when Phase 1 modules are rebuilt")]
    [InlineData("Modules/Products/Views/ProductsView.xaml")]
    public void View_UsesEmptyStateOverlay(string relativePath)
    {
        var path = Path.Combine(SolutionRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"{relativePath} not found");

        var content = File.ReadAllText(path);
        Assert.Contains("EmptyStateOverlay", content, StringComparison.Ordinal);
        Assert.Contains("ItemCount=", content, StringComparison.Ordinal);
    }

    // ══════════════════════════════════════════════════════════════════
    //  No inline empty-state TextBlock + DataTrigger pattern
    // ══════════════════════════════════════════════════════════════════

    [Theory(Skip = "No DataGrid views exist yet — re-enable when Phase 1 modules are rebuilt")]
    [InlineData("Modules/Products/Views/ProductsView.xaml")]
    public void View_NoInlineEmptyStateTextBlock(string relativePath)
    {
        var path = Path.Combine(SolutionRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var content = File.ReadAllText(path);

        // The old pattern: TextBlock with "No {items} found" + DataTrigger on Count
        var oldPattern = InlineEmptyStateRegex();
        var matches = oldPattern.Matches(content);

        Assert.True(matches.Count == 0,
            $"{relativePath} still has {matches.Count} inline empty-state TextBlock(s). " +
            "Use <controls:EmptyStateOverlay .../> instead.");
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

    // ══════════════════════════════════════════════════════════════════
    //  Action wiring — views with add commands wire the button
    // ══════════════════════════════════════════════════════════════════

    [Theory(Skip = "No DataGrid views exist yet — re-enable when Phase 1 modules are rebuilt")]
    [InlineData("Modules/Products/Views/ProductsView.xaml")]
    public void View_WithAddCommand_WiresActionButton(string relativePath)
    {
        var path = Path.Combine(SolutionRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var content = File.ReadAllText(path);

        Assert.Contains("ActionCommand=", content, StringComparison.Ordinal);
        Assert.Contains("ActionText=", content, StringComparison.Ordinal);
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
