using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class ElevationLayerStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void DesignSystem_Should_Define_Semantic_Elevation_Effects()
    {
        var designSystem = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml"));

        Assert.Contains("<x:Null x:Key=\"ElevationEffectBase\"/>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<DropShadowEffect x:Key=\"ElevationEffectCard\"", designSystem, StringComparison.Ordinal);
        Assert.Contains("<DropShadowEffect x:Key=\"ElevationEffectFlyout\"", designSystem, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedCard_And_Flyout_Surfaces_Should_Use_Semantic_Elevation_Effects()
    {
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentCardStyle\" TargetType=\"Border\">",
            "<Setter Property=\"Effect\"          Value=\"{StaticResource ElevationEffectCard}\"/>");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentCardElevatedStyle\" TargetType=\"Border\">",
            "<Setter Property=\"Effect\"          Value=\"{StaticResource ElevationEffectFlyout}\"/>");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FlyoutPopupSurfaceStyle\" TargetType=\"Border\">",
            "<Setter Property=\"Effect\" Value=\"{StaticResource ElevationEffectFlyout}\"/>");

        AssertStyleBlockContains(
            globalStyles,
            "<Style x:Key=\"CardStyle\" TargetType=\"Border\">",
            "<Setter Property=\"Effect\"      Value=\"{StaticResource ElevationEffectCard}\"/>");

        AssertStyleBlockContains(
            globalStyles,
            "<Style x:Key=\"FormCardStyle\" TargetType=\"Border\"",
            "<Setter Property=\"Effect\" Value=\"{StaticResource ElevationEffectBase}\"/>");

        AssertStyleBlockContains(
            globalStyles,
            "<Style x:Key=\"SectionCardStyle\" TargetType=\"Border\"",
            "<Setter Property=\"Effect\" Value=\"{StaticResource ElevationEffectBase}\"/>");

        Assert.Contains("Effect=\"{StaticResource ElevationEffectFlyout}\"", fluentTheme, StringComparison.Ordinal);
    }

    private static void AssertStyleBlockContains(
        string content,
        string styleStart,
        params string[] expectedSnippets)
    {
        var start = content.IndexOf(styleStart, StringComparison.Ordinal);
        Assert.True(start >= 0, "Style block was not found: " + styleStart);

        var end = content.IndexOf("</Style>", start, StringComparison.Ordinal);
        Assert.True(end > start, "Style block did not terminate: " + styleStart);

        var block = content[start..(end + "</Style>".Length)];
        foreach (var snippet in expectedSnippets)
        {
            Assert.Contains(snippet, block, StringComparison.Ordinal);
        }
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
