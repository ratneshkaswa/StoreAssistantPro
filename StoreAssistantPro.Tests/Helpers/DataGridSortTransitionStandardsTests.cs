namespace StoreAssistantPro.Tests.Helpers;

public sealed class DataGridSortTransitionStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Shared_DataGrid_Sort_Transition_Helper_Should_Exist()
    {
        var helperSource = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "DataGridSortTransition.cs"));

        Assert.Contains("class DataGridSortTransition", helperSource, StringComparison.Ordinal);
        Assert.Contains("IsEnabledProperty", helperSource, StringComparison.Ordinal);
        Assert.Contains("dataGrid.Sorting += OnDataGridSorting", helperSource, StringComparison.Ordinal);
        Assert.Contains("TimeSpan.FromMilliseconds(100)", helperSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_DataGrid_Style_Should_Enable_Sort_Transitions()
    {
        var styleSource = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("h:DataGridSortTransition.IsEnabled", styleSource, StringComparison.Ordinal);
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
