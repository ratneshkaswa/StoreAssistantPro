namespace StoreAssistantPro.Tests.Helpers;

public sealed class ExpandableTextStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Shared_Expandable_Text_Control_Should_Define_Show_More_Surface()
    {
        var controlSource = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "ExpandableTextBlock.cs"));
        var styleSource = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("class ExpandableTextBlock", controlSource, StringComparison.Ordinal);
        Assert.Contains("CollapsedLineCountProperty", controlSource, StringComparison.Ordinal);
        Assert.Contains("ShowMoreTextProperty", controlSource, StringComparison.Ordinal);
        Assert.Contains("TargetType=\"controls:ExpandableTextBlock\"", styleSource, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"PART_ContentHost\"", styleSource, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"PART_ToggleButton\"", styleSource, StringComparison.Ordinal);
        Assert.Contains("Show more", styleSource, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Modules\\MainShell\\Views\\MainWindow.xaml", "CollapsedLineCount=\"2\"")]
    [InlineData("Modules\\Billing\\Views\\SaleHistoryView.xaml", "CollapsedLineCount=\"3\"")]
    public void Long_Text_Surfaces_Should_Use_Expandable_Text_Control(string relativePath, string lineCountSnippet)
    {
        var source = File.ReadAllText(Path.Combine(SolutionRoot, relativePath));

        Assert.Contains("controls:ExpandableTextBlock", source, StringComparison.Ordinal);
        Assert.Contains(lineCountSnippet, source, StringComparison.Ordinal);
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
