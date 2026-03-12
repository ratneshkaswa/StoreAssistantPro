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
    public void FirmWindow_Should_Not_Use_WrapPanel_For_SummaryCards()
    {
        var content = ReadFirmWindow();

        Assert.DoesNotContain("<WrapPanel", content, StringComparison.Ordinal);
        Assert.Contains("FirmSummaryCardStyle", content, StringComparison.Ordinal);
    }

    [Fact]
    public void FirmWindow_Should_Use_Padded_InsetCards_For_InnerPanels()
    {
        var content = ReadFirmWindow();

        Assert.Contains("FirmInsetCardStyle", content, StringComparison.Ordinal);
        Assert.Contains("Padding=\"{StaticResource CardContentPadding}\"", content, StringComparison.Ordinal);
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
