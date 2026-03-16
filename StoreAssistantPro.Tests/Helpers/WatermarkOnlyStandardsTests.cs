using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class WatermarkOnlyStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void SharedHintStyles_Should_Be_Collapsed()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("<Style x:Key=\"SetupSectionHintTextStyle\" TargetType=\"TextBlock\">", content, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Visibility\" Value=\"Collapsed\"/>", content, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"HintTextStyle\" TargetType=\"TextBlock\">", content, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"CollapsibleHintTextStyle\" TargetType=\"TextBlock\">", content, StringComparison.Ordinal);
    }

    [Fact]
    public void SmartTooltips_Should_Be_Disabled_By_Default()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Services", "AppStateService.cs"));

        Assert.Contains("SmartTooltipsEnabled = false;", content, StringComparison.Ordinal);
        Assert.Contains("SmartTooltip.GlobalEnabled = false;", content, StringComparison.Ordinal);
        Assert.DoesNotContain("SmartTooltipsEnabled = true;", content, StringComparison.Ordinal);
        Assert.DoesNotContain("SmartTooltip.GlobalEnabled = true;", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Login_Shell_Should_Not_Show_HelperTooltips()
    {
        var loginContent = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "LoginView.xaml"));

        Assert.DoesNotContain("ToolTip=", loginContent, StringComparison.Ordinal);
    }

    [Fact]
    public void SystemSettings_Should_Not_Show_RestoreCaution_Instructions()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Settings", "Views", "SystemSettingsView.xaml"));

        Assert.DoesNotContain("Restore caution", content, StringComparison.Ordinal);
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