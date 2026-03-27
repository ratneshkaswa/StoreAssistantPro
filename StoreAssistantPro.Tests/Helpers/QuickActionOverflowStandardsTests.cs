namespace StoreAssistantPro.Tests.Helpers;

public sealed class QuickActionOverflowStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void MainWindow_Should_Host_QuickAction_Overflow_Flyout()
    {
        var mainWindowXaml = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));

        Assert.Contains("QuickActionOverflowButton", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("OverflowQuickActions", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("VisibleQuickActions", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("OnQuickActionsViewportSizeChanged", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("OnQuickActionOverflowItemClick", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AnchoredFlyoutPopupStyle}\"", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FlyoutMenuSurfaceStyle}\"", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource ToolbarLinkButtonStyle}\"", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("MaxWidth=\"104\"", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("MaxWidth=\"156\"", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("TextTrimming=\"CharacterEllipsis\"", mainWindowXaml, StringComparison.Ordinal);
    }

    [Fact]
    public void QuickActions_Should_Bind_Active_State_Into_Accent_Icon_Styling()
    {
        var mainWindowXaml = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));
        var posStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "PosStyles.xaml"));

        Assert.Contains("Tag=\"{Binding IsActive}\"", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("<Trigger Property=\"Tag\" Value=\"True\">", posStyles, StringComparison.Ordinal);
        Assert.Contains("Value=\"{StaticResource FluentAccentDefault}\"", posStyles, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"QuickActionButtonStyle\"", posStyles, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"QuickActionOverflowItemStyle\"", mainWindowXaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainViewModel_Should_Separate_QuickAccess_Utilities_From_Rail_Navigation()
    {
        var mainViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "ViewModels", "MainViewModel.cs"));

        Assert.Contains("QuickAccessActions", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("ShouldShowInQuickAccessBar", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("ShouldShowInNavigationRail", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("QuickAccessHelpKeys", mainViewModel, StringComparison.Ordinal);
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
