namespace StoreAssistantPro.Tests.Helpers;

public sealed class QuickAccessWindowStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    private static readonly string[] QuickAccessWindows =
    [
        "Modules\\Firm\\Views\\FirmWindow.xaml",
        "Modules\\Users\\Views\\UsersWindow.xaml",
        "Modules\\Tax\\Views\\TaxManagementWindow.xaml",
        "Modules\\Vendors\\Views\\VendorManagementWindow.xaml",
        "Modules\\Products\\Views\\ProductManagementWindow.xaml",
        "Modules\\Categories\\Views\\CategoryManagementWindow.xaml",
        "Modules\\Brands\\Views\\BrandManagementWindow.xaml",
        "Modules\\FinancialYears\\Views\\FinancialYearWindow.xaml",
        "Modules\\Settings\\Views\\SystemSettingsWindow.xaml",
        "Modules\\Inward\\Views\\InwardEntryWindow.xaml",
        "Modules\\Inventory\\Views\\InventoryWindow.xaml",
        "Modules\\Billing\\Views\\BillingWindow.xaml",
        "Modules\\Billing\\Views\\SaleHistoryWindow.xaml",
        "Modules\\Customers\\Views\\CustomerManagementWindow.xaml",
        "Modules\\PurchaseOrders\\Views\\PurchaseOrderWindow.xaml"
    ];

    [Fact]
    public void QuickAccessWindows_Should_Use_DisplayText_And_ContinueTabNavigation()
    {
        var violations = QuickAccessWindows
            .Select(relativePath => new
            {
                RelativePath = relativePath,
                Content = File.ReadAllText(Path.Combine(SolutionRoot, relativePath))
            })
            .Where(file =>
                !file.Content.Contains("TextOptions.TextFormattingMode=\"Display\"", StringComparison.Ordinal)
                || !file.Content.Contains("TextOptions.TextRenderingMode=\"ClearType\"", StringComparison.Ordinal)
                || !file.Content.Contains("KeyboardNavigation.TabNavigation=\"Continue\"", StringComparison.Ordinal))
            .Select(file => file.RelativePath)
            .OrderBy(path => path)
            .ToList();

        Assert.True(
            violations.Count == 0,
            "Quick-access windows must use display text rendering and continuous keyboard tab navigation.\n"
            + string.Join("\n", violations));
    }

    [Fact]
    public void QuickAccessWindows_Should_Not_Force_LogicalDataGridScrolling()
    {
        var violations = QuickAccessWindows
            .Select(relativePath => new
            {
                RelativePath = relativePath,
                Content = File.ReadAllText(Path.Combine(SolutionRoot, relativePath))
            })
            .Where(file => file.Content.Contains("CanContentScroll=\"True\"", StringComparison.Ordinal))
            .Select(file => file.RelativePath)
            .OrderBy(path => path)
            .ToList();

        Assert.True(
            violations.Count == 0,
            "Quick-access windows should rely on the shared pixel-scroll grid standard, not force logical scrolling.\n"
            + string.Join("\n", violations));
    }

    [Fact]
    public void SegmentedQuickAccessWindows_Should_Use_WrapPanel_TabRows()
    {
        var segmentedWindows = new[]
        {
            "Modules\\Tax\\Views\\TaxManagementWindow.xaml",
            "Modules\\Categories\\Views\\CategoryManagementWindow.xaml",
            "Modules\\Inventory\\Views\\InventoryWindow.xaml"
        };

        var violations = segmentedWindows
            .Select(relativePath => new
            {
                RelativePath = relativePath,
                Content = File.ReadAllText(Path.Combine(SolutionRoot, relativePath))
            })
            .Where(file =>
                !file.Content.Contains("<WrapPanel Grid.Row=\"1\"", StringComparison.Ordinal)
                || file.Content.Contains("<StackPanel Grid.Row=\"1\" Orientation=\"Horizontal\"", StringComparison.Ordinal))
            .Select(file => file.RelativePath)
            .OrderBy(path => path)
            .ToList();

        Assert.True(
            violations.Count == 0,
            "Segmented quick-access windows must use WrapPanel tab rows so the tab bar stays responsive.\n"
            + string.Join("\n", violations));
    }

    [Fact]
    public void DataHeavyQuickAccessWindows_Should_Use_EnterpriseGridStyle_On_ReadOnlyLists()
    {
        var gridWindows = new[]
        {
            "Modules\\Brands\\Views\\BrandManagementWindow.xaml",
            "Modules\\Categories\\Views\\CategoryManagementWindow.xaml",
            "Modules\\Customers\\Views\\CustomerManagementWindow.xaml",
            "Modules\\FinancialYears\\Views\\FinancialYearWindow.xaml",
            "Modules\\Inventory\\Views\\InventoryWindow.xaml",
            "Modules\\Products\\Views\\ProductManagementWindow.xaml",
            "Modules\\PurchaseOrders\\Views\\PurchaseOrderWindow.xaml",
            "Modules\\Tax\\Views\\TaxManagementWindow.xaml",
            "Modules\\Users\\Views\\UsersWindow.xaml",
            "Modules\\Vendors\\Views\\VendorManagementWindow.xaml",
            "Modules\\Billing\\Views\\SaleHistoryWindow.xaml"
        };

        var violations = gridWindows
            .Select(relativePath => new
            {
                RelativePath = relativePath,
                Content = File.ReadAllText(Path.Combine(SolutionRoot, relativePath))
            })
            .Where(file => !file.Content.Contains("Style=\"{StaticResource EnterpriseDataGridStyle}\"", StringComparison.Ordinal))
            .Select(file => file.RelativePath)
            .OrderBy(path => path)
            .ToList();

        Assert.True(
            violations.Count == 0,
            "Data-heavy quick-access windows must use the shared enterprise grid style for their read-only lists.\n"
            + string.Join("\n", violations));
    }

    [Fact]
    public void QuickAccessToggleCells_Should_Pass_The_Clicked_Row_As_CommandParameter()
    {
        var sharedToggleTemplate = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "ToggleSwitch.xaml"));
        var categoryWindow = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Categories", "Views", "CategoryManagementWindow.xaml"));
        var taxWindow = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Tax", "Views", "TaxManagementWindow.xaml"));

        Assert.Contains("CommandParameter=\"{Binding}\"", sharedToggleTemplate, StringComparison.Ordinal);
        Assert.Contains("CommandParameter=\"{Binding}\"", categoryWindow, StringComparison.Ordinal);
        Assert.Contains("CommandParameter=\"{Binding}\"", taxWindow, StringComparison.Ordinal);
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
