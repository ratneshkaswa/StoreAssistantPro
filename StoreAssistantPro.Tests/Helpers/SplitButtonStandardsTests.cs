using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class SplitButtonStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void SharedStyles_Should_Define_SplitButton_Contract()
    {
        var control = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "SplitButton.cs"));
        var styles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("class SplitButton", control, StringComparison.Ordinal);
        Assert.Contains("PART_DropDownButton", control, StringComparison.Ordinal);
        Assert.Contains("menu.IsOpen = true;", control, StringComparison.Ordinal);
        Assert.Contains("SplitButtonPrimarySegmentStyle", styles, StringComparison.Ordinal);
        Assert.Contains("SplitButtonChevronSegmentStyle", styles, StringComparison.Ordinal);
        Assert.Contains("SplitButtonFlyoutActionStyle", styles, StringComparison.Ordinal);
        Assert.Contains("<Style TargetType=\"controls:SplitButton\">", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void PurchaseOrders_Should_Use_SplitButton_For_Create_Action()
    {
        var purchaseOrderView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "PurchaseOrders", "Views", "PurchaseOrderView.xaml"));

        Assert.Contains("<controls:SplitButton", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Content=\"Create PO\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding CreateOrderCommand}\"", purchaseOrderView, StringComparison.Ordinal);
        Assert.Contains("Duplicate selected order", purchaseOrderView, StringComparison.Ordinal);
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
