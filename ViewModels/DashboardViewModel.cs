using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Services;

namespace StoreAssistantPro.ViewModels;

public partial class DashboardViewModel(IProductService productService, ISalesService salesService) : ObservableObject
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
        var products = await productService.GetAllAsync();
        var productList = products.ToList();

        TotalProducts = productList.Count;
        LowStockCount = productList.Count(p => p.Quantity <= 5);

        var todaysSalesList = await salesService.GetSalesByDateRangeAsync(DateTime.Today, DateTime.Today.AddDays(1));
        var salesList = todaysSalesList.ToList();

        TodaysSales = salesList.Sum(s => s.TotalAmount);
        TodaysTransactions = salesList.Count;
    }
}
