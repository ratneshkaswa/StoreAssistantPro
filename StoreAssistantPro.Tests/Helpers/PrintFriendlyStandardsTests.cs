using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class PrintFriendlyStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void PrintDocuments_Should_Use_Black_On_White_Output()
    {
        var reportHelper = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Printing", "ReportPrintHelper.cs"));
        var barcodeLabels = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "BarcodeLabels", "ViewModels", "BarcodeLabelViewModel.cs"));

        Assert.Contains("Foreground = Brushes.Black", reportHelper, StringComparison.Ordinal);
        Assert.Contains("Background = Brushes.White", barcodeLabels, StringComparison.Ordinal);
        Assert.Contains("Foreground = Brushes.Black", barcodeLabels, StringComparison.Ordinal);
        Assert.DoesNotContain("Brushes.DimGray", barcodeLabels, StringComparison.Ordinal);
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
