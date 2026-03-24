using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class ErrorStateOverlayStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void SharedLoadError_Path_Should_Expose_Retry_Overlay_Contract()
    {
        var baseViewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Base", "BaseViewModel.cs"));
        var overlayControl = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "ErrorStateOverlay.cs"));
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("LoadErrorMessage", baseViewModel, StringComparison.Ordinal);
        Assert.Contains("HasLoadError", baseViewModel, StringComparison.Ordinal);
        Assert.Contains("LoadErrorMessage = ex.Message;", baseViewModel, StringComparison.Ordinal);
        Assert.Contains("class ErrorStateOverlay", overlayControl, StringComparison.Ordinal);
        Assert.Contains("ActionText", overlayControl, StringComparison.Ordinal);
        Assert.Contains("<Style TargetType=\"controls:ErrorStateOverlay\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("Retry load", globalStyles, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadHeavy_Pages_Should_Show_Retry_Overlay_On_Load_Failure()
    {
        var grn = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "GRN", "Views", "GRNManagementView.xaml"));
        var quotations = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Quotations", "Views", "QuotationManagementView.xaml"));
        var purchaseOrders = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "PurchaseOrders", "Views", "PurchaseOrderView.xaml"));
        var inward = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inward", "Views", "InwardEntryView.xaml"));

        Assert.Contains("<controls:ErrorStateOverlay", grn, StringComparison.Ordinal);
        Assert.Contains("ActionCommand=\"{Binding LoadCommand}\"", grn, StringComparison.Ordinal);
        Assert.Contains("<controls:ErrorStateOverlay", quotations, StringComparison.Ordinal);
        Assert.Contains("ActionCommand=\"{Binding LoadCommand}\"", quotations, StringComparison.Ordinal);
        Assert.Contains("<controls:ErrorStateOverlay", purchaseOrders, StringComparison.Ordinal);
        Assert.Contains("ActionCommand=\"{Binding LoadCommand}\"", purchaseOrders, StringComparison.Ordinal);
        Assert.Contains("<controls:ErrorStateOverlay", inward, StringComparison.Ordinal);
        Assert.Contains("ActionCommand=\"{Binding LoadCommand}\"", inward, StringComparison.Ordinal);
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
