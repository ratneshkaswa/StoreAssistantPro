using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class PrintPreviewStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void PrintPreviewWindow_Should_Use_InApp_CommandBar_Navigation_And_Zoom()
    {
        var xaml = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Printing", "PrintPreviewWindow.xaml"));

        Assert.Contains("Style=\"{StaticResource FluentToolbarStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"PreviousPageButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"NextPageButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"PageStatusText\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ZoomSlider\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ZoomStatusText\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"OnFitWidthClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"OnPrintClick\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintPreviewWindow_Should_Wire_PageState_And_Print_Action()
    {
        var code = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Printing", "PrintPreviewWindow.xaml.cs"));

        Assert.Contains("DocViewer.PageViewsChanged += (_, _) => UpdateViewerState();", code, StringComparison.Ordinal);
        Assert.Contains("DocViewer.PreviousPage();", code, StringComparison.Ordinal);
        Assert.Contains("DocViewer.NextPage();", code, StringComparison.Ordinal);
        Assert.Contains("DocViewer.FitToMaxPagesAcross(1);", code, StringComparison.Ordinal);
        Assert.Contains("var dialog = new PrintDialog();", code, StringComparison.Ordinal);
        Assert.Contains("dialog.PrintDocument(_document.DocumentPaginator, TitleText.Text);", code, StringComparison.Ordinal);
        Assert.Contains("DocViewer.Zoom = PrintPreviewZoomState.Get(_zoomStateKey);", code, StringComparison.Ordinal);
        Assert.Contains("PrintPreviewZoomState.Set(_zoomStateKey, zoom);", code, StringComparison.Ordinal);
        Assert.Contains("PageStatusText.Text =", code, StringComparison.Ordinal);
        Assert.Contains("ZoomStatusText.Text =", code, StringComparison.Ordinal);
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
