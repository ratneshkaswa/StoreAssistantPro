using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.MainShell.Services;

namespace StoreAssistantPro.Modules.MainShell.ViewModels;

public partial class MainWorkspaceViewModel(IDashboardService dashboardService) : BaseViewModel
{
    [ObservableProperty]
    public partial int TotalProducts { get; set; }

    [ObservableProperty]
    public partial int LowStockCount { get; set; }

    [ObservableProperty]
    public partial decimal TodaysSales { get; set; }

    [ObservableProperty]
    public partial int TodaysTransactions { get; set; }

    public ObservableCollection<Sale> RecentSales { get; } = [];

    public ObservableCollection<Product> LowStockProducts { get; } = [];

    [RelayCommand]
    private Task LoadMainWorkspaceAsync() => RunLoadAsync(async _ =>
    {
        var summary = await dashboardService.GetSummaryAsync();

        TotalProducts = summary.TotalProducts;
        LowStockCount = summary.LowStockCount;
        TodaysSales = summary.TodaysSales;
        TodaysTransactions = summary.TodaysTransactions;

        RecentSales.Clear();
        foreach (var sale in summary.RecentSales)
            RecentSales.Add(sale);

        LowStockProducts.Clear();
        foreach (var product in summary.LowStockProducts)
            LowStockProducts.Add(product);
    });
}
