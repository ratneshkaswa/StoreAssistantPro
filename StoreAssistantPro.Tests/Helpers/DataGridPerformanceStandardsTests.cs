namespace StoreAssistantPro.Tests.Helpers;

public sealed class DataGridPerformanceStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Shared_DataGrid_Styles_Should_Enable_Group_Virtualization_And_Cache_Window()
    {
        var content = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("<Setter Property=\"VirtualizingPanel.IsVirtualizingWhenGrouping\" Value=\"True\"/>", content, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"VirtualizingPanel.CacheLength\" Value=\"1,1\"/>", content, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"EnableColumnVirtualization\" Value=\"True\"/>", content, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"AnalyticalReportDataGridStyle\" TargetType=\"DataGrid\"", content, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"ScrollViewer.IsDeferredScrollingEnabled\" Value=\"True\"/>", content, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Could not find solution root from " + AppContext.BaseDirectory);
    }
}
