namespace StoreAssistantPro.Tests.Helpers;

public sealed class InteractionHarnessStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Shell_And_Custom_Controls_Should_Expose_Named_Interaction_Surfaces()
    {
        var mainWindow = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));
        var splitButtonStyle = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));
        var breadcrumb = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Controls", "BreadcrumbBar.cs"));

        Assert.Contains("x:Name=\"NotificationBellButton\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"CommandPaletteSearchBox\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"CommandPaletteResultsList\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("PART_PrimaryButton", splitButtonStyle, StringComparison.Ordinal);
        Assert.Contains("PART_DropDownButton", splitButtonStyle, StringComparison.Ordinal);
        Assert.Contains("PART_Button", breadcrumb, StringComparison.Ordinal);
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
