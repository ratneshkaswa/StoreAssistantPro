namespace StoreAssistantPro.Tests.Helpers;

public sealed class MotionStaggerStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Motion_Should_Support_Automatic_Item_Staggering()
    {
        var motion = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "Motion.cs"));

        Assert.Contains("StaggerChildrenProperty", motion, StringComparison.Ordinal);
        Assert.Contains("StaggerIndexProperty", motion, StringComparison.Ordinal);
        Assert.Contains("private static void OnNoOpChanged", motion, StringComparison.Ordinal);
        Assert.Contains("DispatcherPriority.Background", motion, StringComparison.Ordinal);
    }

    [Fact]
    public void MainShell_Surfaces_Should_Enable_Automatic_Staggering()
    {
        var mainWindow = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));
        var workspace = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "WorkspaceView.xaml"));

        Assert.Contains("CommandPaletteResultsList", mainWindow, StringComparison.Ordinal);
        Assert.Contains("h:Motion.StaggerChildren=\"True\"", mainWindow, StringComparison.Ordinal);
        Assert.True(
            CountOccurrences(workspace, "h:Motion.StaggerChildren=\"True\"") >= 2,
            "Workspace list surfaces should retain the current stagger opt-in wiring.");
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
