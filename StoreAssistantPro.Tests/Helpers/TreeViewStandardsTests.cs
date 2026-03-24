using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class TreeViewStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void TreeViewTheme_Should_Provide_Dotted_Indent_Guides()
    {
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("<Style TargetType=\"{x:Type TreeViewItem}\">", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"IndentGuide\"", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("TileMode=\"Tile\"", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"TreeChevronGlyph\"", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("SubtleFillColorSecondary", fluentTheme, StringComparison.Ordinal);
    }

    private static string FindSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0 ||
                Directory.GetFiles(dir, "*.slnx").Length > 0)
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException(
            "Could not find solution root from " + AppContext.BaseDirectory);
    }
}
