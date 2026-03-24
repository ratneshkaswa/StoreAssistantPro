using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class DataGridStarColumnSizingStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Star_Column_Sizing_Helper_Should_Track_Star_Weights_And_Apply_Pixel_Widths()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "DataGridStarColumnSizing.cs"));

        Assert.Contains("DataGridLengthUnitType.Star", source, StringComparison.Ordinal);
        Assert.Contains("_starWeights", source, StringComparison.Ordinal);
        Assert.Contains("availableWidth", source, StringComparison.Ordinal);
        Assert.Contains("new DataGridLength(proportionalWidth, DataGridLengthUnitType.Pixel)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_DataGrid_Styles_Should_Enable_Proportional_Star_Sizing()
    {
        var styles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("h:DataGridStarColumnSizing.IsEnabled", styles, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"EnterpriseDataGridStyle\"", styles, StringComparison.Ordinal);
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
