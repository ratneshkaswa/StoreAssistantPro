namespace StoreAssistantPro.Tests.Helpers;

public sealed class ShellScrollChromeStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void ResponsiveContentControl_Should_Expose_VerticalScrollOffset_And_Scroll_Event()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "ResponsiveContentControl.cs"));

        Assert.Contains("VerticalScrollOffsetProperty", source, StringComparison.Ordinal);
        Assert.Contains("public double VerticalScrollOffset", source, StringComparison.Ordinal);
        Assert.Contains("public event ScrollChangedEventHandler? ScrollOffsetChanged", source, StringComparison.Ordinal);
        Assert.Contains("_scrollViewer.ScrollChanged += OnScrollViewerScrollChanged", source, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollOffset = e.VerticalOffset;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_Should_Name_Scroll_Driven_QuickAction_Chrome()
    {
        var xaml = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));

        Assert.Contains("x:Name=\"QuickActionBarHost\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"QuickActionBar\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"QuickActionBarShadow\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ShellContentHost\"", xaml, StringComparison.Ordinal);
        Assert.Contains("CommandBarScrollShadowBrush", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_CodeBehind_Should_AutoHide_And_Shadow_CommandBar_On_Scroll()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml.cs"));

        Assert.Contains("QuickActionBarAutoHideThreshold", source, StringComparison.Ordinal);
        Assert.Contains("QuickActionBarRevealThreshold", source, StringComparison.Ordinal);
        Assert.Contains("OnShellContentScrollChanged", source, StringComparison.Ordinal);
        Assert.Contains("UpdateQuickActionBarShadow", source, StringComparison.Ordinal);
        Assert.Contains("SetQuickActionBarVisible(false)", source, StringComparison.Ordinal);
        Assert.Contains("SetQuickActionBarVisible(true)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DesignSystem_Should_Define_CommandBarScrollShadowBrush()
    {
        var tokens = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml"));

        Assert.Contains("x:Key=\"CommandBarScrollShadowBrush\"", tokens, StringComparison.Ordinal);
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
