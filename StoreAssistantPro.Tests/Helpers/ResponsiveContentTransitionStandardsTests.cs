using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class ResponsiveContentTransitionStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void ResponsiveContentHost_Should_Use_Clipped_Snapshot_Overlay()
    {
        var app = File.ReadAllText(Path.Combine(SolutionRoot, "App.xaml"));

        Assert.Contains("x:Name=\"PART_TransitionHost\"", app, StringComparison.Ordinal);
        Assert.Contains("ClipToBounds=\"True\"", app, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"PART_PreviousSnapshot\"", app, StringComparison.Ordinal);
        Assert.Contains("Source=\"{TemplateBinding PreviousSnapshot}\"", app, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"PART_CurrentPresenter\"", app, StringComparison.Ordinal);
        Assert.Contains("Content=\"{TemplateBinding Content}\"", app, StringComparison.Ordinal);
    }

    [Fact]
    public void ResponsiveContentControl_Should_Animate_Snapshot_Page_Exit_With_Shared_Tokens()
    {
        var controlCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "ResponsiveContentControl.cs"));
        var designSystem = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml"));

        Assert.Contains("PreviousSnapshotProperty", controlCode, StringComparison.Ordinal);
        Assert.Contains("CaptureCurrentSnapshot", controlCode, StringComparison.Ordinal);
        Assert.Contains("RenderTargetBitmap", controlCode, StringComparison.Ordinal);
        Assert.Contains("FluentDurationPageExit", controlCode, StringComparison.Ordinal);
        Assert.Contains("MotionSlideOffsetExitSmall", controlCode, StringComparison.Ordinal);
        Assert.Contains("TranslateTransform.YProperty", controlCode, StringComparison.Ordinal);
        Assert.Contains("PreviousSnapshot = snapshot;", controlCode, StringComparison.Ordinal);
        Assert.Contains("PreviousSnapshot = null;", controlCode, StringComparison.Ordinal);

        Assert.Contains("<Duration x:Key=\"FluentDurationPageExit\">0:0:0.10</Duration>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"MotionSlideOffsetExitSmall\">-8</sys:Double>", designSystem, StringComparison.Ordinal);
    }

    [Fact]
    public void ResponsiveContentControl_Should_Crossfade_New_Content_During_View_Switches()
    {
        var controlCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "ResponsiveContentControl.cs"));
        var designSystem = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml"));

        Assert.Contains("StartCurrentEnterTransition", controlCode, StringComparison.Ordinal);
        Assert.Contains("FluentDurationViewSwitchEnter", controlCode, StringComparison.Ordinal);
        Assert.Contains("FluentDurationViewSwitchOverlap", controlCode, StringComparison.Ordinal);
        Assert.Contains("_currentPresenter.Opacity = 0;", controlCode, StringComparison.Ordinal);
        Assert.Contains("BeginTime = overlap", controlCode, StringComparison.Ordinal);
        Assert.Contains("CompleteCurrentEnter", controlCode, StringComparison.Ordinal);

        Assert.Contains("<Duration x:Key=\"FluentDurationViewSwitchEnter\">0:0:0.15</Duration>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Duration x:Key=\"FluentDurationViewSwitchOverlap\">0:0:0.05</Duration>", designSystem, StringComparison.Ordinal);
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
