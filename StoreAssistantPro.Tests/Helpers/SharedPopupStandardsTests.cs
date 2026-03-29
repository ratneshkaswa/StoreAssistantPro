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
    public void FlyoutPopups_Should_Use_Shared_Static_Chrome_Without_Animated_Entrance()
    {
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));
        var popupMotion = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "PopupFlyoutMotion.cs"));

        Assert.Contains("AnchoredFlyoutPopupStyle", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("FlyoutPopupSurfaceStyle", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("FlyoutMenuSurfaceStyle", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("h:PopupFlyoutMotion.IsEnabled", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("OnNoOpChanged", popupMotion, StringComparison.Ordinal);
        Assert.Contains("speed-first mode avoids popup event hooks", popupMotion, StringComparison.Ordinal);
        Assert.DoesNotContain("popup.Opened +=", popupMotion, StringComparison.Ordinal);
        Assert.DoesNotContain("popup.Closed +=", popupMotion, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation", popupMotion, StringComparison.Ordinal);
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
    public void HelpHintTooltips_Should_Use_Pointed_TeachingTip_Surface()
    {
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));
        var smartTooltip = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "SmartTooltip.cs"));
        var helpHint = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "HelpHint.cs"));

        Assert.Contains("TeachingTipTooltipStyle", globalStyles, StringComparison.Ordinal);
        Assert.Contains("UseCalloutStyle", smartTooltip, StringComparison.Ordinal);
        Assert.Contains("SetUseCalloutStyle(fe, helpText is not null);", helpHint, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_Tooltip_Templates_Should_Open_Without_Local_Fade_Or_Slide_Storyboards()
    {
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.DoesNotContain("BeginStoryboard x:Name=\"OpenStoryboard\"", globalStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("RoutedEvent=\"Opened\"", globalStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("RenderTransform.Y", globalStyles, StringComparison.Ordinal);
    }

    [Fact]
    public void SmartTooltip_RuntimeContent_Should_Enable_CrispLayout_And_TextRendering()
    {
        var smartTooltip = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "SmartTooltip.cs"));

        Assert.Contains("UseLayoutRounding = true", smartTooltip, StringComparison.Ordinal);
        Assert.Contains("SnapsToDevicePixels = true", smartTooltip, StringComparison.Ordinal);
        Assert.Contains("TextOptions.SetTextFormattingMode", smartTooltip, StringComparison.Ordinal);
        Assert.Contains("TextFormattingMode.Display", smartTooltip, StringComparison.Ordinal);
        Assert.Contains("TextOptions.SetTextRenderingMode", smartTooltip, StringComparison.Ordinal);
        Assert.Contains("TextRenderingMode.ClearType", smartTooltip, StringComparison.Ordinal);
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
