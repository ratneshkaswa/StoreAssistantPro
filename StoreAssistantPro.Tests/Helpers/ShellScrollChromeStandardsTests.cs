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
        Assert.Contains("x:Name=\"ShellContentPanel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<controls:ResponsiveContentControl Content=\"{Binding CurrentView}\"/>", xaml, StringComparison.Ordinal);
        Assert.Contains("CommandBarScrollShadowBrush", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_CodeBehind_Should_Keep_CommandBar_Pinned_And_Only_Update_Shadow_On_Scroll()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml.cs"));

        Assert.Contains("OnShellContentScrollChanged", source, StringComparison.Ordinal);
        Assert.Contains("UpdateQuickActionBarShadow", source, StringComparison.Ordinal);
        Assert.DoesNotContain("QuickActionBarAutoHideThreshold", source, StringComparison.Ordinal);
        Assert.DoesNotContain("QuickActionBarRevealThreshold", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SetQuickActionBarVisible(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_CodeBehind_Should_Apply_QuickAction_Chrome_Instantly_Without_Scroll_Animations()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml.cs"));

        Assert.Contains("QuickActionBarHost.MaxHeight = double.PositiveInfinity;", source, StringComparison.Ordinal);
        Assert.Contains("QuickActionBarHost.Opacity = 1;", source, StringComparison.Ordinal);
        Assert.Contains("QuickActionBarTransform.Y = 0;", source, StringComparison.Ordinal);
        Assert.Contains("QuickActionBarShadow.Opacity = targetOpacity;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("QuickActionBarShadow.BeginAnimation(OpacityProperty, animation);", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SetQuickActionBarVisible(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_CodeBehind_Should_Apply_Navigation_Rail_Width_Instantly()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml.cs"));

        Assert.Contains("ApplyNavigationRailWidth", source, StringComparison.Ordinal);
        Assert.Contains("NavigationRailHost.BeginAnimation(FrameworkElement.WidthProperty, null);", source, StringComparison.Ordinal);
        Assert.Contains("NavigationRailHost.Width = targetWidth;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new DoubleAnimation(targetWidth", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DesignSystem_Should_Define_CommandBarScrollShadowBrush()
    {
        var tokens = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml"));

        Assert.Contains("x:Key=\"CommandBarScrollShadowBrush\"", tokens, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_ScrollViewer_Chrome_Should_Use_Static_Scrollbar_Visibility()
    {
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("<Style x:Key=\"FluentVerticalScrollBar\" TargetType=\"ScrollBar\">", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Opacity\" Value=\"1\"/>", fluentTheme, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation Storyboard.TargetName=\"PART_VerticalScrollBar\"", fluentTheme, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation Storyboard.TargetName=\"PART_HorizontalScrollBar\"", fluentTheme, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTime=\"0:0:1\"", fluentTheme, StringComparison.Ordinal);
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
