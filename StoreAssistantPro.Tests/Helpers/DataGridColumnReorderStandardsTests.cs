namespace StoreAssistantPro.Tests.Helpers;

public sealed class DataGridColumnReorderStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Shared_DataGrid_Styles_Should_Explicitly_Enable_Column_Reordering()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("Style TargetType=\"DataGrid\"", source, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"EnterpriseDataGridStyle\"", source, StringComparison.Ordinal);
        Assert.Equal(
            2,
            source.Split("CanUserReorderColumns\" Value=\"True\"", StringSplitOptions.None).Length - 1);
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
