namespace StoreAssistantPro.Tests.Helpers;

public sealed class SpeedFirstMotionStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Shared_Global_Styles_Should_Avoid_Remaining_Command_Row_And_Fab_Animations()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.DoesNotContain("CommandSpinnerStoryboard", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FabScale", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation Storyboard.TargetName=\"SelectionIndicator\"", source, StringComparison.Ordinal);
        Assert.Contains("<Setter TargetName=\"SelectionIndicator\" Property=\"Opacity\" Value=\"1\"/>", source, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Opacity\" Value=\"1\"/>", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_Toggle_And_Focus_Templates_Should_Use_Static_State_Changes()
    {
        var toggleStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "ToggleSwitch.xaml"));
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("<Setter TargetName=\"Thumb\" Property=\"HorizontalAlignment\" Value=\"Right\"/>", toggleStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation Storyboard.TargetName=\"ThumbTranslate\"", toggleStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation Storyboard.TargetName=\"FocusIndicator\"", fluentTheme, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation Storyboard.TargetName=\"ChevronRotate\"", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter TargetName=\"Chevron\" Property=\"Text\" Value=\"&#xE70E;\"/>", fluentTheme, StringComparison.Ordinal);
    }

    [Fact]
    public void Active_Filter_Chips_Should_Not_Animate_Scale()
    {
        var posStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "PosStyles.xaml"));
        var chipStyle = GetStyleBlock(posStyles, "<Style x:Key=\"ActiveFilterChipButtonStyle\" TargetType=\"Button\">");

        Assert.DoesNotContain("ChipScale", chipStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("MotionButtonScale", chipStyle, StringComparison.Ordinal);
    }

    private static string GetStyleBlock(string source, string marker)
    {
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
            return source;

        var end = source.IndexOf("</Style>", start, StringComparison.Ordinal);
        if (end < 0)
            return source[start..];

        return source[start..(end + "</Style>".Length)];
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
