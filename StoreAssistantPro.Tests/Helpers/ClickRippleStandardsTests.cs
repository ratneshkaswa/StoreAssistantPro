using Xunit;
using System.Text.RegularExpressions;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class ClickRippleStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void ClickRipple_Should_Retain_Attached_Property_But_Avoid_Ripple_Animation()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "ClickRipple.cs"));

        Assert.Contains("PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;", source, StringComparison.Ordinal);
        Assert.Contains("Intentionally no-op", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AdornerLayer.GetAdornerLayer", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RippleAdorner", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_Button_Styles_Should_Not_Opt_Into_Click_Ripples()
    {
        var theme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("x:Key=\"FluentAccentButtonStyle\"", theme, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"FluentStandardButtonStyle\"", theme, StringComparison.Ordinal);
        Assert.Empty(
            Regex.Matches(theme, "h:ClickRipple\\.IsEnabled").Cast<Match>());
    }

    [Fact]
    public void Shared_Button_Styles_Should_Not_Animate_Scale_On_Hover_Or_Press()
    {
        var theme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        var accentStyle = GetStyleBlock(theme, "<Style x:Key=\"FluentAccentButtonStyle\" TargetType=\"Button\">");
        var standardStyle = GetStyleBlock(theme, "<Style x:Key=\"FluentStandardButtonStyle\" TargetType=\"Button\">");
        var subtleStyle = GetStyleBlock(theme, "<Style x:Key=\"FluentSubtleButtonStyle\" TargetType=\"Button\">");

        Assert.DoesNotContain("ScaleTransform x:Name=\"BdScale\"", accentStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("Storyboard.TargetName=\"BdScale\"", accentStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("MotionButtonScale", accentStyle, StringComparison.Ordinal);

        Assert.DoesNotContain("ScaleTransform x:Name=\"BdScale\"", standardStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("Storyboard.TargetName=\"BdScale\"", standardStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("MotionButtonScale", standardStyle, StringComparison.Ordinal);

        Assert.DoesNotContain("ScaleTransform x:Name=\"BdScale\"", subtleStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("Storyboard.TargetName=\"BdScale\"", subtleStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("MotionButtonScale", subtleStyle, StringComparison.Ordinal);
    }

    private static string GetStyleBlock(string content, string styleStart)
    {
        var start = content.IndexOf(styleStart, StringComparison.Ordinal);
        Assert.True(start >= 0, "Style block was not found: " + styleStart);

        var end = content.IndexOf("</Style>", start, StringComparison.Ordinal);
        Assert.True(end > start, "Style block did not terminate: " + styleStart);

        return content[start..(end + "</Style>".Length)];
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
