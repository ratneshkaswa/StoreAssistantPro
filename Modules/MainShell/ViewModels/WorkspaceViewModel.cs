using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.MainShell.Models;
using StoreAssistantPro.Modules.MainShell.Services;

namespace StoreAssistantPro.Modules.MainShell.ViewModels;

public partial class WorkspaceViewModel(IDashboardService dashboardService) : BaseViewModel
{
    [ObservableProperty]
    public partial int TotalProducts { get; set; }

    [ObservableProperty]
    public partial int LowStockCount { get; set; }

    [ObservableProperty]
    public partial int OutOfStockCount { get; set; }

    [ObservableProperty]
    public partial int OverstockCount { get; set; }

    [ObservableProperty]
    public partial decimal InventoryValue { get; set; }

    [ObservableProperty]
    public partial decimal InventoryValueAtSale { get; set; }

    [ObservableProperty]
    public partial decimal TodaysSales { get; set; }

    [ObservableProperty]
    public partial int TodaysTransactions { get; set; }

    [ObservableProperty]
    public partial decimal TodaysAverageSale { get; set; }

    [ObservableProperty]
    public partial decimal TodaysTotalDiscount { get; set; }

    public ObservableCollection<Sale> RecentSales { get; } = [];

    public ObservableCollection<Product> LowStockProducts { get; } = [];

    public ObservableCollection<BrandLowStockCount> LowStockByBrand { get; } = [];

    public ObservableCollection<Product> OutOfStockProducts { get; } = [];

    public ObservableCollection<BrandInventoryValue> InventoryValueByBrand { get; } = [];

    public ObservableCollection<PaymentMethodSales> SalesByPaymentMethod { get; } = [];

    public ObservableCollection<TopSellingProduct> TopSellingProducts { get; } = [];

    [RelayCommand]
    private Task LoadMainWorkspaceAsync() => RunLoadAsync(async _ =>
    {
        var summary = await dashboardService.GetSummaryAsync();

        TotalProducts = summary.TotalProducts;
        LowStockCount = summary.LowStockCount;
        OutOfStockCount = summary.OutOfStockCount;
        OverstockCount = summary.OverstockCount;
        InventoryValue = summary.InventoryValue;
        InventoryValueAtSale = summary.InventoryValueAtSale;
        TodaysSales = summary.TodaysSales;
        TodaysTransactions = summary.TodaysTransactions;
        TodaysAverageSale = summary.TodaysAverageSale;
        TodaysTotalDiscount = summary.TodaysTotalDiscount;

        RecentSales.Clear();
        foreach (var sale in summary.RecentSales)
            RecentSales.Add(sale);

        LowStockProducts.Clear();
        foreach (var product in summary.LowStockProducts)
            LowStockProducts.Add(product);

        LowStockByBrand.Clear();
        foreach (var entry in summary.LowStockByBrand)
            LowStockByBrand.Add(entry);

        OutOfStockProducts.Clear();
        foreach (var product in summary.OutOfStockProducts)
            OutOfStockProducts.Add(product);

        InventoryValueByBrand.Clear();
        foreach (var entry in summary.InventoryValueByBrand)
            InventoryValueByBrand.Add(entry);

        SalesByPaymentMethod.Clear();
        foreach (var entry in summary.SalesByPaymentMethod)
            SalesByPaymentMethod.Add(entry);

        TopSellingProducts.Clear();
        foreach (var entry in summary.TopSellingProducts)
            TopSellingProducts.Add(entry);
    });
}
