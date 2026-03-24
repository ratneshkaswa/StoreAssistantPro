using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class FluentIconFontStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void DesignSystem_Should_Use_SegoeFluentIcons_Without_Mdl2Fallback()
    {
        var designSystem = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml"));

        Assert.Contains(
            "<FontFamily x:Key=\"FluentIconFont\">Segoe Fluent Icons</FontFamily>",
            designSystem,
            StringComparison.Ordinal);
        Assert.DoesNotContain("Segoe MDL2 Assets", designSystem, StringComparison.Ordinal);
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
