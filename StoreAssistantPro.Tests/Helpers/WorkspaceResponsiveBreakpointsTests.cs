namespace StoreAssistantPro.Tests.Helpers;

public sealed class WorkspaceResponsiveBreakpointsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Workspace_View_Should_Collapse_Fixed_Sidebars_Below_Breakpoint()
    {
        var xaml = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "WorkspaceView.xaml"));

        Assert.Contains("x:Key=\"ResponsiveSidebarGapColumnStyle\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"ResponsiveSidebarColumnStyle\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"ResponsiveSidebarPanelStyle\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"ResponsiveSidebarCardStyle\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ConverterParameter=1440", xaml, StringComparison.Ordinal);
        Assert.Equal(
            2,
            CountOccurrences(xaml, "Style=\"{StaticResource ResponsiveSidebarColumnStyle}\""));
        Assert.Equal(1, CountOccurrences(xaml, "Style=\"{StaticResource ResponsiveSidebarPanelStyle}\""));
        Assert.Equal(1, CountOccurrences(xaml, "Style=\"{StaticResource ResponsiveSidebarCardStyle}\""));
        Assert.Contains("Grid.RowSpan=\"2\"", xaml, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string content, string needle)
    {
        var count = 0;
        var index = 0;

        while ((index = content.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
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
