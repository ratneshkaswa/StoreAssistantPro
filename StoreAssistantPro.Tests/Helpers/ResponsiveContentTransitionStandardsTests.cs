using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class ResponsiveContentTransitionStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void ResponsiveContentHost_Should_Use_Static_Scroll_Host_Template()
    {
        var app = File.ReadAllText(Path.Combine(SolutionRoot, "App.xaml"));

        Assert.Contains("x:Name=\"PART_ScrollViewer\"", app, StringComparison.Ordinal);
        Assert.Contains("CanContentScroll=\"False\"", app, StringComparison.Ordinal);
        Assert.Contains("Content=\"{TemplateBinding Content}\"", app, StringComparison.Ordinal);
        Assert.DoesNotContain("PART_TransitionHost", app, StringComparison.Ordinal);
        Assert.DoesNotContain("PART_PreviousSnapshot", app, StringComparison.Ordinal);
        Assert.DoesNotContain("PART_CurrentPresenter", app, StringComparison.Ordinal);
    }

    [Fact]
    public void ResponsiveContentControl_Should_Be_A_Static_Scroll_Aware_Content_Host()
    {
        var controlCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "ResponsiveContentControl.cs"));

        Assert.Contains("VerticalScrollOffsetProperty", controlCode, StringComparison.Ordinal);
        Assert.Contains("PART_ScrollViewer", controlCode, StringComparison.Ordinal);
        Assert.Contains("ScrollOffsetChanged", controlCode, StringComparison.Ordinal);
        Assert.DoesNotContain("PreviousSnapshotProperty", controlCode, StringComparison.Ordinal);
        Assert.DoesNotContain("TransitionsEnabledProperty", controlCode, StringComparison.Ordinal);
        Assert.DoesNotContain("RenderTargetBitmap", controlCode, StringComparison.Ordinal);
    }

    [Fact]
    public void ResponsiveContentControl_Should_Reset_Scroll_Position_On_Content_Change()
    {
        var controlCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "ResponsiveContentControl.cs"));

        Assert.Contains("_scrollViewer?.ScrollToTop();", controlCode, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollOffset = 0;", controlCode, StringComparison.Ordinal);
        Assert.DoesNotContain("StartCurrentEnterTransition", controlCode, StringComparison.Ordinal);
    }

    [Fact]
    public void MainShell_Should_Not_Need_Transition_Disablement_When_Host_Is_Static()
    {
        var controlCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "ResponsiveContentControl.cs"));
        var mainWindowCodeBehind = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml.cs"));

        Assert.DoesNotContain("TransitionsEnabledProperty", controlCode, StringComparison.Ordinal);
        Assert.DoesNotContain("TransitionsEnabled", controlCode, StringComparison.Ordinal);
        Assert.DoesNotContain("ShellContentHost.TransitionsEnabled = false;", mainWindowCodeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void DesignSystem_Should_Use_Speed_First_Motion_Tokens()
    {
        var designSystem = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml"));
        var motionSystem = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "MotionSystem.xaml"));

        Assert.Contains("<Duration x:Key=\"FluentDurationFast\">0:0:0</Duration>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Duration x:Key=\"FluentDurationNormal\">0:0:0</Duration>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Duration x:Key=\"FluentDurationSlow\">0:0:0</Duration>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Duration x:Key=\"FluentDurationPageFade\">0:0:0</Duration>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"MotionScaleHoverFrom\">0.992</sys:Double>", designSystem, StringComparison.Ordinal);
        Assert.DoesNotContain("<Storyboard", motionSystem, StringComparison.Ordinal);
        Assert.DoesNotContain("<BeginStoryboard", motionSystem, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation", motionSystem, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_UserControl_Style_Should_Keep_Page_Fade_Off_By_Default()
    {
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("<Style TargetType=\"UserControl\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"h:Motion.PageFadeIn\" Value=\"False\"/>", globalStyles, StringComparison.Ordinal);
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
