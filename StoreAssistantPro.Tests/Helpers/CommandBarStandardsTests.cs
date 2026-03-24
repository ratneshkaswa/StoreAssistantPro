namespace StoreAssistantPro.Tests.Helpers;

public sealed class CommandBarStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Shared_Toolbar_Style_Should_Use_Win11_CommandBar_Chrome()
    {
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("<Style x:Key=\"FluentToolbarStyle\" TargetType=\"Border\">", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Background\"         Value=\"{StaticResource ChromeAltFillColorSecondary}\"/>", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"BorderThickness\"    Value=\"0,0,0,1\"/>", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Padding\"            Value=\"16,10\"/>", fluentTheme, StringComparison.Ordinal);
    }

    [Fact]
    public void Enterprise_Page_Layout_Should_Place_Toolbar_Above_Content()
    {
        var appXaml = File.ReadAllText(
            Path.Combine(SolutionRoot, "App.xaml"));

        Assert.Contains("<!-- Row 1: Toolbar -->", appXaml, StringComparison.Ordinal);
        Assert.Contains("<ContentPresenter x:Name=\"PART_Toolbar\" Grid.Row=\"1\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("<!-- Row 2: Main Content + Loading Overlay -->", appXaml, StringComparison.Ordinal);
        Assert.Contains("<ContentPresenter x:Name=\"PART_StatusBar\" Grid.Row=\"5\"", appXaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Main_Window_Quick_Action_Bar_Should_Use_Shared_Toolbar_Style()
    {
        var mainWindow = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));

        Assert.Contains("<!-- QUICK ACTION BAR -->", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FluentToolbarStyle}\"", mainWindow, StringComparison.Ordinal);
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
