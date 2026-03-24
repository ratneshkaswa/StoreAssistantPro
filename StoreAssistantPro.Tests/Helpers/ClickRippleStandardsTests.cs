using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class ClickRippleStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void ClickRipple_Should_Use_An_Adorner_And_200ms_Ripple_Timing()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "ClickRipple.cs"));

        Assert.Contains("PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;", source, StringComparison.Ordinal);
        Assert.Contains("AdornerLayer.GetAdornerLayer", source, StringComparison.Ordinal);
        Assert.Contains("new RippleAdorner", source, StringComparison.Ordinal);
        Assert.Contains("TimeSpan.FromMilliseconds(200)", source, StringComparison.Ordinal);
        Assert.Contains("DrawEllipse", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_Button_Styles_Should_Enable_Click_Ripples()
    {
        var theme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("x:Key=\"FluentAccentButtonStyle\"", theme, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"FluentStandardButtonStyle\"", theme, StringComparison.Ordinal);
        Assert.Contains("h:ClickRipple.IsEnabled", theme, StringComparison.Ordinal);
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
