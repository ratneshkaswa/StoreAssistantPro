namespace StoreAssistantPro.Tests.Helpers;

public sealed class ColumnChooserStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Shared_Column_Chooser_Helper_Should_Exist()
    {
        var source = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Helpers", "DataGridColumnChooser.cs"));

        Assert.Contains("TargetProperty", source, StringComparison.Ordinal);
        Assert.Contains("ContextMenu", source, StringComparison.Ordinal);
        Assert.Contains("MenuItem", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Modules\\Billing\\Views\\SaleHistoryView.xaml", "SaleHistoryGrid")]
    [InlineData("Modules\\Products\\Views\\ProductManagementView.xaml", "ProductsGrid")]
    public void Main_Grid_Toolbars_Should_Expose_Column_Chooser(string relativePath, string gridName)
    {
        var source = File.ReadAllText(Path.Combine(SolutionRoot, relativePath));

        Assert.Contains($"x:Name=\"{gridName}\"", source, StringComparison.Ordinal);
        Assert.Contains("DataGridColumnChooser.Target", source, StringComparison.Ordinal);
        Assert.Contains("Choose columns", source, StringComparison.Ordinal);
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
