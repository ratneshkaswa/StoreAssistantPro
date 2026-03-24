using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class TouchKeyboardFocusStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void SharedHelper_Should_Bring_Focused_Inputs_Into_View()
    {
        var helper = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "BringFocusedFieldIntoView.cs"));

        Assert.Contains("Keyboard.GotKeyboardFocusEvent", helper, StringComparison.Ordinal);
        Assert.Contains("DispatcherPriority.Input", helper, StringComparison.Ordinal);
        Assert.Contains("BringIntoView(new Rect(", helper, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedInputStyles_Should_Enable_Touch_Focus_Scrolling()
    {
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));
        var theme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("h:BringFocusedFieldIntoView.IsEnabled\" Value=\"True\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("TargetType=\"PasswordBox\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("TargetType=\"ComboBox\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("TargetType=\"DatePicker\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("FluentSearchTextBoxStyle", globalStyles, StringComparison.Ordinal);
        Assert.Contains("TargetType=\"{x:Type controls:NumberBox}\"", theme, StringComparison.Ordinal);
        Assert.Contains("h:BringFocusedFieldIntoView.IsEnabled\" Value=\"True\"", theme, StringComparison.Ordinal);
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
