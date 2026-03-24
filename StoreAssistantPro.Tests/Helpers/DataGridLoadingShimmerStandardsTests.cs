using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class DataGridLoadingShimmerStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void LoadingOverlay_Should_Contain_Table_Header_And_Row_Placeholders()
    {
        var theme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("<Border Grid.Row=\"3\"", theme, StringComparison.Ordinal);
        Assert.Contains("<Grid Grid.Row=\"2\" Margin=\"0,24,0,0\">", theme, StringComparison.Ordinal);
        Assert.Contains("<Grid Grid.Row=\"4\" Margin=\"0,0,0,12\">", theme, StringComparison.Ordinal);
        Assert.Contains("SkeletonPlaceholderSecondaryBrush", theme, StringComparison.Ordinal);
        Assert.Contains("SkeletonPlaceholderBrush", theme, StringComparison.Ordinal);
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
