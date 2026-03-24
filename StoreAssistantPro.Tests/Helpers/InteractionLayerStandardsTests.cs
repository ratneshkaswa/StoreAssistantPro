namespace StoreAssistantPro.Tests.Helpers;

public sealed class InteractionLayerStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void PressStateTracker_Should_Handle_Press_Lifecycle()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "PressStateTracker.cs"));

        Assert.Contains("PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;", content, StringComparison.Ordinal);
        Assert.Contains("PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;", content, StringComparison.Ordinal);
        Assert.Contains("MouseLeave += OnMouseLeave;", content, StringComparison.Ordinal);
        Assert.Contains("LostMouseCapture += OnLostMouseCapture;", content, StringComparison.Ordinal);
        Assert.Contains("FrameworkPropertyMetadataOptions.Inherits", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_Cards_And_Rows_Should_Use_Pressed_State_Layers()
    {
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("<Setter Property=\"h:PressStateTracker.IsEnabled\" Value=\"True\"/>", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Trigger Property=\"h:PressStateTracker.IsPressed\" Value=\"True\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("Value=\"{StaticResource ControlAltFillColorSecondary}\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("Value=\"{StaticResource SubtleFillColorTertiary}\"", globalStyles, StringComparison.Ordinal);
    }

    [Fact]
    public void CommandPalette_List_Items_Should_Use_Pressed_State_Layers()
    {
        var mainWindow = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));

        Assert.Contains("h:PressStateTracker.IsEnabled", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Property=\"h:PressStateTracker.IsPressed\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("CommandPaletteResultItemStyle", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void Page_Header_Template_Should_Right_Align_Header_Actions()
    {
        var appXaml = File.ReadAllText(
            Path.Combine(SolutionRoot, "App.xaml"));

        Assert.Contains("Content=\"{TemplateBinding HeaderContent}\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("DockPanel.Dock=\"Right\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("HorizontalAlignment=\"Right\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("VerticalAlignment=\"Center\"", appXaml, StringComparison.Ordinal);
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
