namespace StoreAssistantPro.Tests.Helpers;

public sealed class CommandPaletteStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void MainWindow_Should_Host_CommandPalette_Overlay()
    {
        var mainWindowXaml = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));

        Assert.Contains("CommandPaletteSearchBox", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("CommandPaletteResultsList", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("CloseCommandPaletteCommand", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("OnCommandPalettePreviewKeyDown", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.Name=\"Close command palette\"", mainWindowXaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainViewModel_Should_Register_CommandPalette_Shortcut()
    {
        var mainViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "ViewModels", "MainViewModel.cs"));

        Assert.Contains("Title = \"Command Palette\"", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("Gesture = \"Ctrl+K\"", mainViewModel, StringComparison.Ordinal);
        Assert.Contains("Description = \"Search pages, tools, and recent actions\"", mainViewModel, StringComparison.Ordinal);
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
