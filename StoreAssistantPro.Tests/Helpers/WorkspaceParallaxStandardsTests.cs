namespace StoreAssistantPro.Tests.Helpers;

public sealed class WorkspaceParallaxStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void WorkspaceView_Should_Declare_Scroll_Driven_Hero_Backdrop()
    {
        var xaml = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "WorkspaceView.xaml"));

        Assert.Contains("x:Name=\"WorkspaceScrollViewer\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ScrollChanged=\"OnWorkspaceScrollChanged\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"HeroBackdropParallaxTransform\"", xaml, StringComparison.Ordinal);
        Assert.Contains("FluentAccentLight3", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkspaceView_CodeBehind_Should_Move_Hero_Backdrop_At_Half_Scroll_Speed()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "WorkspaceView.xaml.cs"));

        Assert.Contains("OnWorkspaceScrollChanged", source, StringComparison.Ordinal);
        Assert.Contains("HeroBackdropParallaxTransform.Y", source, StringComparison.Ordinal);
        Assert.Contains("e.VerticalOffset * 0.5", source, StringComparison.Ordinal);
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
