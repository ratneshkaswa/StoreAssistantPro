using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public class SetupWindowComplianceTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    private static string FindSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0 || Directory.GetFiles(dir, "*.slnx").Length > 0)
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Could not find solution root from " + AppContext.BaseDirectory);
    }

    [Fact]
    public void SetupWindow_Should_KeepScrollableContent_WhenFieldsGrow()
    {
        var file = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupWindow.xaml");
        var xaml = File.ReadAllText(file);

        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", xaml);
        Assert.Contains("h:SmoothScroll.IsEnabled=\"False\"", xaml);
    }

    [Fact]
    public void SetupWindow_Should_NotUseWatermarkAdorners()
    {
        var file = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupWindow.xaml");
        var xaml = File.ReadAllText(file);

        Assert.DoesNotContain("h:Watermark.Text=", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupWindow_Should_ProvideAccessibilityLabels()
    {
        var file = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupWindow.xaml");
        var xaml = File.ReadAllText(file);

        Assert.Contains("AutomationProperties.LabeledBy=\"{Binding ElementName=FirmNameLabel}\"", xaml);
        Assert.Contains("AutomationProperties.LabeledBy=\"{Binding ElementName=MasterPinLabel}\"", xaml);
        Assert.Contains("AutomationProperties.LabeledBy=\"{Binding ElementName=UserConfirmLabel}\"", xaml);
    }

    [Fact]
    public void Codebase_Should_NotUseMicaFlag()
    {
        var forbidden = "use" + "MicaAlt";
        var csFiles = Directory.EnumerateFiles(SolutionRoot, "*.cs", SearchOption.AllDirectories);
        foreach (var file in csFiles)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain(forbidden, text, StringComparison.Ordinal);
        }
    }
}
