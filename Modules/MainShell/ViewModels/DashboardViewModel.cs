using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Modules.MainShell.Services;

namespace StoreAssistantPro.Modules.MainShell.ViewModels;

public partial class DashboardViewModel(IDashboardService dashboardService) : BaseViewModel
{
    [ObservableProperty]
    public partial int TotalProducts { get; set; }

    [ObservableProperty]
    public partial int LowStockCount { get; set; }

    [ObservableProperty]
    public partial decimal TodaysSales { get; set; }

    [ObservableProperty]
    public partial int TodaysTransactions { get; set; }

    [RelayCommand]
    private async Task LoadDashboardAsync()
    {
        var summary = await dashboardService.GetSummaryAsync();

        TotalProducts = summary.TotalProducts;
        LowStockCount = summary.LowStockCount;
        TodaysSales = summary.TodaysSales;
        TodaysTransactions = summary.TodaysTransactions;
    }
}
