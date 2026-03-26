namespace StoreAssistantPro.Tests.Helpers;

public sealed class ConnectedAnimationStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void ResponsiveContentHost_Should_Expose_A_Connected_Snapshot_Surface()
    {
        var appXaml = File.ReadAllText(
            Path.Combine(SolutionRoot, "App.xaml"));
        var hostCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "ResponsiveContentControl.cs"));
        var designSystem = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml"));

        Assert.Contains("PART_ConnectedSnapshot", appXaml, StringComparison.Ordinal);
        Assert.Contains("PART_ConnectedSnapshot", hostCode, StringComparison.Ordinal);
        Assert.Contains("ConnectedNavigationSource.TryConsume", hostCode, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"FluentDurationConnectedAnimation\"", designSystem, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_Navigation_Surfaces_Should_Capture_Connected_Animation_Sources()
    {
        var posStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "PosStyles.xaml"));
        var mainWindow = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));

        Assert.Contains("h:ConnectedNavigationSource.IsEnabled\" Value=\"True\"", posStyles, StringComparison.Ordinal);
        Assert.Contains("QuickActionOverflowItemStyle", mainWindow, StringComparison.Ordinal);
        Assert.Contains("NavigationRailActionButtonStyle", mainWindow, StringComparison.Ordinal);
        Assert.Contains("CommandPaletteResultItemStyle", mainWindow, StringComparison.Ordinal);
        Assert.Contains("h:ConnectedNavigationSource.IsEnabled\" Value=\"True\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void Active_Filter_Chips_Should_Use_Animated_Dismiss_Behavior()
    {
        var posStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "PosStyles.xaml"));
        var helper = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "AnimatedDismissButton.cs"));

        Assert.Contains("x:Key=\"ActiveFilterChipButtonStyle\"", posStyles, StringComparison.Ordinal);
        Assert.Contains("h:AnimatedDismissButton.IsEnabled\" Value=\"True\"", posStyles, StringComparison.Ordinal);
        Assert.Contains("new DoubleAnimation(width, 0", helper, StringComparison.Ordinal);
        Assert.Contains("new DoubleAnimation(1, 0", helper, StringComparison.Ordinal);
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
