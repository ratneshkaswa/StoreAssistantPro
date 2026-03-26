namespace StoreAssistantPro.Tests.Helpers;

public sealed class SearchPerformanceStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Shared_SearchStyle_Should_Enable_Debounced_Search()
    {
        var content = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("<Setter Property=\"h:DebouncedSearch.IsEnabled\" Value=\"True\"/>", content, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"h:DebouncedSearch.Delay\" Value=\"0:0:0.25\"/>", content, StringComparison.Ordinal);
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
