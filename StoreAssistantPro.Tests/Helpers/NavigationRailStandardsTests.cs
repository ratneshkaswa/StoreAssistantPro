namespace StoreAssistantPro.Tests.Helpers;

public sealed class NavigationRailStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void MainWindow_Should_Use_Top_Menu_And_QuickAccess_Instead_Of_A_Sidebar_Rail()
    {
        var source = File.ReadAllText(Path.Combine(
            SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));

        Assert.DoesNotContain("NavigationRailHost", source, StringComparison.Ordinal);
        Assert.DoesNotContain("NavigationRailActionButtonStyle", source, StringComparison.Ordinal);
        Assert.DoesNotContain("NavigationRailToggleButtonStyle", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ToggleNavigationRailOrNavigateBackCommand", source, StringComparison.Ordinal);
        Assert.Contains("Header=\"_Tools\"", source, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"QuickActionsViewport\"", source, StringComparison.Ordinal);
        Assert.Contains("VisibleQuickActions", source, StringComparison.Ordinal);
        Assert.Contains("OverflowQuickActions", source, StringComparison.Ordinal);
        Assert.DoesNotContain("<BeginStoryboard>", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RotateTransform", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MainViewModel_Should_Not_Expose_Rail_State_And_Back_Mode()
    {
        var source = File.ReadAllText(Path.Combine(
            SolutionRoot, "Modules", "MainShell", "ViewModels", "MainViewModel.cs"));

        Assert.DoesNotContain("IsNavigationRailExpanded", source, StringComparison.Ordinal);
        Assert.DoesNotContain("NavigationRailWidth", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IsNavigationRailBackMode", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ToggleNavigationRailOrNavigateBack()", source, StringComparison.Ordinal);
        Assert.Contains("QuickAccessActions", source, StringComparison.Ordinal);
        Assert.Contains("VisibleQuickActions", source, StringComparison.Ordinal);
        Assert.Contains("OverflowQuickActions", source, StringComparison.Ordinal);
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
