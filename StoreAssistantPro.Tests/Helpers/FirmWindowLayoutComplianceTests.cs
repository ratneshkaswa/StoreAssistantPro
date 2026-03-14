using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class FirmWindowLayoutComplianceTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void FirmWindow_Should_Use_ViewportBound_ContentWidth()
    {
        var content = ReadFirmWindow();

        Assert.Contains(
            "Width=\"{Binding ViewportWidth, RelativeSource={RelativeSource AncestorType=ScrollViewer}}\"",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void FirmWindow_Should_Not_Use_KpiSummaryCards()
    {
        var content = ReadFirmWindow();

        Assert.DoesNotContain("FirmSummaryCardStyle", content, StringComparison.Ordinal);
        Assert.DoesNotContain("FluentKpiBlueMuted", content, StringComparison.Ordinal);
        Assert.DoesNotContain("AutomationProperties.Name=\"Business summary card\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void FirmWindow_Should_Not_Render_Removed_Inline_Summary_Blocks()
    {
        var content = ReadFirmWindow();

        Assert.DoesNotContain("FirmInsetCardStyle", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Compliance Summary", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Allow billing with negative stock", content, StringComparison.Ordinal);
    }

    [Fact]
    public void FirmWindow_Should_Allow_Horizontal_Overflow_Recovery()
    {
        var content = ReadFirmWindow();

        Assert.Contains("HorizontalScrollBarVisibility=\"Auto\"", content, StringComparison.Ordinal);
        Assert.Contains("PanningMode=\"Both\"", content, StringComparison.Ordinal);
        Assert.DoesNotContain("h:SmoothScroll.IsEnabled=\"True\"", content, StringComparison.Ordinal);
        Assert.DoesNotContain("CanContentScroll=\"True\"", content, StringComparison.Ordinal);
    }

    private static string ReadFirmWindow() =>
        File.ReadAllText(Path.Combine(
            SolutionRoot,
            "Modules",
            "Firm",
            "Views",
            "FirmWindow.xaml"));

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
