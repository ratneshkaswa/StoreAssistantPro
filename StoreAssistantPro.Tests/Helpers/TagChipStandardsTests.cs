namespace StoreAssistantPro.Tests.Helpers;

public sealed class TagChipStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Shared_Tag_Chip_Resources_Should_Exist()
    {
        var posStyles = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Styles", "PosStyles.xaml"));
        var appXaml = File.ReadAllText(Path.Combine(SolutionRoot, "App.xaml"));

        Assert.Contains("x:Key=\"SemanticTagChipBorderStyle\"", posStyles, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"SemanticTagChipTextStyle\"", posStyles, StringComparison.Ordinal);
        Assert.Contains("TagChipBrushConverter", appXaml, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Modules\\Billing\\Views\\SaleHistoryView.xaml")]
    [InlineData("Modules\\Customers\\Views\\CustomerManagementView.xaml")]
    [InlineData("Modules\\Expenses\\Views\\ExpenseManagementView.xaml")]
    [InlineData("Modules\\Products\\Views\\ProductManagementView.xaml")]
    [InlineData("Modules\\Reports\\Views\\ReportsView.xaml")]
    public void Classification_Views_Should_Use_Semantic_Tag_Chips(string relativePath)
    {
        var source = File.ReadAllText(Path.Combine(SolutionRoot, relativePath));

        Assert.Contains("SemanticTagChipBorderStyle", source, StringComparison.Ordinal);
        Assert.Contains("SemanticTagChipTextStyle", source, StringComparison.Ordinal);
        Assert.Contains("TagChipBrushConverter", source, StringComparison.Ordinal);
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
