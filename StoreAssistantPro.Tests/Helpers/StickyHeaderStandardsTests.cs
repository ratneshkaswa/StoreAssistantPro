namespace StoreAssistantPro.Tests.Helpers;

public sealed class StickyHeaderStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void DoubleGreaterThanConverter_Should_Use_Strict_GreaterThan_Comparison()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "DoubleGreaterThanConverter.cs"));

        Assert.Contains("return currentValue > threshold;", content, StringComparison.Ordinal);
        Assert.Contains("Binding.DoNothing", content, StringComparison.Ordinal);
    }

    [Fact]
    public void DataGrid_Headers_Should_Show_Shadow_When_Scrolled()
    {
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));
        var designSystem = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml"));

        Assert.Contains("<h:DoubleGreaterThanConverter x:Key=\"DoubleGreaterThanConverter\"/>", globalStyles, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"StickyHeaderShadow\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("RelativeSource={RelativeSource AncestorType=ScrollViewer}", globalStyles, StringComparison.Ordinal);
        Assert.Contains("Converter={StaticResource DoubleGreaterThanConverter}", globalStyles, StringComparison.Ordinal);
        Assert.Contains("Background=\"{StaticResource StickyHeaderShadowBrush}\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<LinearGradientBrush x:Key=\"StickyHeaderShadowBrush\"", designSystem, StringComparison.Ordinal);
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
