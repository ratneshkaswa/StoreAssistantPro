namespace StoreAssistantPro.Tests.Helpers;

public sealed class DataGridLayoutPersistenceStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Shared_Grid_Layouts_Should_Use_Persistence_Behavior_On_Primary_Grids()
    {
        var helper = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Helpers", "DataGridLayoutPersistence.cs"));
        var saleHistory = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "SaleHistoryView.xaml"));
        var products = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Products", "Views", "ProductManagementView.xaml"));
        var variants = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Products", "Views", "VariantManagementView.xaml"));
        var branch = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Branch", "Views", "BranchManagementView.xaml"));
        var brands = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Brands", "Views", "BrandManagementView.xaml"));
        var categories = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Categories", "Views", "CategoryManagementView.xaml"));
        var customers = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementView.xaml"));
        var debtors = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Debtors", "Views", "DebtorManagementView.xaml"));
        var expenses = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Expenses", "Views", "ExpenseManagementView.xaml"));
        var financialYears = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "FinancialYears", "Views", "FinancialYearView.xaml"));
        var grn = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "GRN", "Views", "GRNManagementView.xaml"));
        var inventory = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Inventory", "Views", "InventoryManagementView.xaml"));
        var payments = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Payments", "Views", "PaymentManagementView.xaml"));
        var orders = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Orders", "Views", "OrderManagementView.xaml"));
        var ironing = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Ironing", "Views", "IroningManagementView.xaml"));
        var purchaseOrders = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "PurchaseOrders", "Views", "PurchaseOrderView.xaml"));
        var quotations = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Quotations", "Views", "QuotationManagementView.xaml"));
        var salaries = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Salaries", "Views", "SalaryManagementView.xaml"));
        var salesPurchase = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "SalesPurchase", "Views", "SalesPurchaseView.xaml"));
        var tax = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Tax", "Views", "TaxManagementView.xaml"));
        var users = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Users", "Views", "UserManagementView.xaml"));
        var vendors = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementView.xaml"));

        Assert.Contains("class DataGridLayoutPersistence", helper, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"SaleHistoryGrid\"", saleHistory, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"ProductsGrid\"", products, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"VariantsGrid\"", variants, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"BranchBillsGrid\"", branch, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"BrandsGrid\"", brands, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"CategoryTypesGrid\"", categories, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"CategoriesGrid\"", categories, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"CustomersGrid\"", customers, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"DebtorsGrid\"", debtors, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"ExpensesGrid\"", expenses, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"FinancialYearsGrid\"", financialYears, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"GRNGrid\"", grn, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"InventoryStockOverviewGrid\"", inventory, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"RecentStockTakesGrid\"", inventory, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"StockTakeItemsGrid\"", inventory, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"PaymentsGrid\"", payments, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"OrdersGrid\"", orders, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"IroningEntriesGrid\"", ironing, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"PurchaseOrdersGrid\"", purchaseOrders, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"QuotationsGrid\"", quotations, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"SalariesGrid\"", salaries, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"SalesPurchaseGrid\"", salesPurchase, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"TaxRatesGrid\"", tax, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"TaxGroupsGrid\"", tax, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"TaxGroupSlabsGrid\"", tax, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"HSNCodesGrid\"", tax, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"UsersGrid\"", users, StringComparison.Ordinal);
        Assert.Contains("h:DataGridLayoutPersistence.Key=\"VendorsGrid\"", vendors, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Could not find solution root from " + AppContext.BaseDirectory);
    }
}
