using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class LoadingSkeletonStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void LoadingOverlay_Should_Render_Static_Skeleton_Surface_Without_Shimmer()
    {
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));
        var designSystem = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml"));
        var loadingOverlay = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "LoadingOverlay.cs"));

        Assert.Contains("<Style TargetType=\"{x:Type controls:LoadingOverlay}\">", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("SkeletonPlaceholderBrush", designSystem, StringComparison.Ordinal);
        Assert.Contains("SkeletonPlaceholderSecondaryBrush", designSystem, StringComparison.Ordinal);
        Assert.DoesNotContain("SkeletonSheenTransform", fluentTheme, StringComparison.Ordinal);
        Assert.DoesNotContain("LoadingOverlaySheenStoryboard", fluentTheme, StringComparison.Ordinal);
        Assert.DoesNotContain("RepeatBehavior=\"Forever\"", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("Preparing content", loadingOverlay, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedPageTemplates_Should_Use_LoadingOverlay_Instead_Of_TextOnly_Loading()
    {
        var appXaml = File.ReadAllText(
            Path.Combine(SolutionRoot, "App.xaml"));
        var pageTemplate = File.ReadAllText(
            Path.Combine(SolutionRoot, "Templates", "PageViewTemplate.xaml"));

        Assert.Contains("<controls:LoadingOverlay x:Name=\"PART_Loading\"", appXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Loading...\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("<controls:LoadingOverlay IsActive=\"{Binding IsLoading}\"", pageTemplate, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"⏳ Loading…\"", pageTemplate, StringComparison.Ordinal);
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
