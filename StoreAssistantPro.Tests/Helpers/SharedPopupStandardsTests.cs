using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class SharedPopupStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void SharedPopupTemplates_Should_Avoid_Hardcoded_SubmenuOffsets()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.DoesNotContain("HorizontalOffset=\"-2\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedPopupScrollHosts_Should_Allow_Horizontal_Overflow_Recovery()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("HorizontalScrollBarVisibility=\"Auto\"", content, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void TooltipTemplates_Should_Wrap_Long_Unbroken_Text()
    {
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));
        var smartTooltip = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "SmartTooltip.cs"));

        Assert.Contains("Text=\"{TemplateBinding Content}\"", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("TextWrapping=\"WrapWithOverflow\"", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("TextTrimming=\"None\"", fluentTheme, StringComparison.Ordinal);
        Assert.DoesNotContain("TextBlock.TextWrapping=", fluentTheme, StringComparison.Ordinal);
        Assert.DoesNotContain("TextBlock.TextWrapping=", globalStyles, StringComparison.Ordinal);
        Assert.Contains("TextWrapping = TextWrapping.WrapWithOverflow", smartTooltip, StringComparison.Ordinal);
    }

    [Fact]
    public void MasterPinDialog_Should_Grow_For_LongPromptText()
    {
        var xaml = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Views", "MasterPinDialog.xaml"));
        var codeBehind = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Views", "MasterPinDialog.xaml.cs"));

        Assert.Contains("TextWrapping=\"WrapWithOverflow\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SizeToContent = System.Windows.SizeToContent.Height;", codeBehind, StringComparison.Ordinal);
        Assert.Contains("MinHeight = 0;", codeBehind, StringComparison.Ordinal);
        Assert.Contains("protected override double DialogMinWidth => 320;", codeBehind, StringComparison.Ordinal);
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
