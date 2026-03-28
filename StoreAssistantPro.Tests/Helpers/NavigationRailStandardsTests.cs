namespace StoreAssistantPro.Tests.Helpers;

public sealed class NavigationRailStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void MainWindow_Should_Host_The_Sidebar_Navigation_Rail()
    {
        var source = File.ReadAllText(Path.Combine(
            SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));

        Assert.Contains("NavigationRailHost", source, StringComparison.Ordinal);
        Assert.Contains("NavigationRailActionButtonStyle", source, StringComparison.Ordinal);
        Assert.Contains("NavigationRailToggleButtonStyle", source, StringComparison.Ordinal);
        Assert.Contains("ToggleNavigationRailOrNavigateBackCommand", source, StringComparison.Ordinal);
        Assert.Contains("NotificationBadgeBehavior.Count", source, StringComparison.Ordinal);
        Assert.Contains("DataContext.IsNavigationRailExpanded", source, StringComparison.Ordinal);
        Assert.Contains("TextTrimming=\"CharacterEllipsis\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("<BeginStoryboard>", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RotateTransform", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MainViewModel_Should_Expose_Rail_State_And_Back_Mode()
    {
        var source = File.ReadAllText(Path.Combine(
            SolutionRoot, "Modules", "MainShell", "ViewModels", "MainViewModel.cs"));

        Assert.Contains("IsNavigationRailExpanded", source, StringComparison.Ordinal);
        Assert.Contains("NavigationRailWidth", source, StringComparison.Ordinal);
        Assert.Contains("IsNavigationRailBackMode", source, StringComparison.Ordinal);
        Assert.Contains("ToggleNavigationRailOrNavigateBack()", source, StringComparison.Ordinal);
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
