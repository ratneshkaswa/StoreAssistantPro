using System.Text.RegularExpressions;
using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public class SharedSpacingAndWatermarkComplianceTests
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
    public void GlobalStyles_Should_Enable_Fallback_Watermarks_For_Shared_Inputs()
    {
        var stylesFile = Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml");
        var styles = File.ReadAllText(stylesFile);

        Assert.True(
            Regex.Matches(styles, "h:Watermark.UseFallbackText\" Value=\"True\"", RegexOptions.CultureInvariant).Count >= 4,
            "TextBox, PasswordBox, ComboBox, and DatePicker should enable fallback watermarks in shared styles.");
        Assert.Contains("<Setter Property=\"TextAlignment\" Value=\"Left\"/>", styles, StringComparison.Ordinal);
        Assert.True(
            Regex.Matches(styles, "<Setter Property=\"HorizontalContentAlignment\" Value=\"Left\"/>", RegexOptions.CultureInvariant).Count >= 3,
            "PasswordBox, ComboBox, and DatePicker should enforce left content alignment in shared styles.");
    }

    [Fact]
    public void WatermarkHelper_Should_Support_Shared_Input_Controls()
    {
        var helperFile = Path.Combine(SolutionRoot, "Core", "Helpers", "Watermark.cs");
        var helper = File.ReadAllText(helperFile);

        Assert.DoesNotContain("temporarily disabled app-wide", helper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UseFallbackText", helper, StringComparison.Ordinal);
        Assert.Contains("PasswordBox", helper, StringComparison.Ordinal);
        Assert.Contains("ComboBox", helper, StringComparison.Ordinal);
        Assert.Contains("DatePicker", helper, StringComparison.Ordinal);
        Assert.Contains("GetGridSiblingLabelText", helper, StringComparison.Ordinal);
        Assert.Contains("TryGetHostedContentBounds", helper, StringComparison.Ordinal);
        Assert.Contains("PART_ContentHost", helper, StringComparison.Ordinal);
        Assert.Contains("\"ContentSite\"", helper, StringComparison.Ordinal);
        Assert.Contains("GetTextAlignment", helper, StringComparison.Ordinal);
        Assert.Contains("FormattedText", helper, StringComparison.Ordinal);
        Assert.Contains("drawingContext.DrawText", helper, StringComparison.Ordinal);
        Assert.Contains("UseLayoutRounding = true", helper, StringComparison.Ordinal);
        Assert.DoesNotContain("control.HorizontalContentAlignment switch", helper, StringComparison.Ordinal);
    }

    [Fact]
    public void DesignSystem_Should_Use_Roomier_Global_Spacing()
    {
        var tokensFile = Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml");
        var tokens = File.ReadAllText(tokensFile);

        Assert.Contains("<Thickness x:Key=\"DialogPadding\">32</Thickness>", tokens, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"CardPadding\">16</Thickness>", tokens, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"FieldGroupSpacing\">0,0,0,16</Thickness>", tokens, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"ItemSpacing\">0,0,0,12</Thickness>", tokens, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"FormColumnGap\">0,0,16,0</Thickness>", tokens, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"CardContentPadding\">16,12</Thickness>", tokens, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"ControlPadding\">12,8</Thickness>", tokens, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"ControlHeight\">44</sys:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"ButtonHeight\">40</sys:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"SetupButtonHeight\">40</sys:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"FieldWidthCompact\">104</sys:Double>", tokens, StringComparison.Ordinal);
    }
}
