using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class LoadingOverlayPresentationStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void LoadingOverlay_Should_Use_Compact_Progress_Surface_Instead_Of_Skeleton_Frame()
    {
        var theme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("<Style TargetType=\"{x:Type controls:LoadingOverlay}\">", theme, StringComparison.Ordinal);
        Assert.Contains("<controls:ProgressRing IsActive=\"True\"", theme, StringComparison.Ordinal);
        Assert.Contains("Text=\"{TemplateBinding Message}\"", theme, StringComparison.Ordinal);
        Assert.Contains("Please wait a moment.", theme, StringComparison.Ordinal);
        Assert.DoesNotContain("SkeletonPlaceholderSecondaryBrush", theme, StringComparison.Ordinal);
        Assert.DoesNotContain("SkeletonPlaceholderBrush", theme, StringComparison.Ordinal);
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
