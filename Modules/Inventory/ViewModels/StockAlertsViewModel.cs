using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Inventory.Services;

namespace StoreAssistantPro.Modules.Inventory.ViewModels;

public partial class StockAlertsViewModel(
    IStockAlertService stockAlertService) : BaseViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<StockAlert> Alerts { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Product> LowStockProducts { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Product> OverStockProducts { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedAlert))]
    public partial StockAlert? SelectedAlert { get; set; }

    public bool HasSelectedAlert => SelectedAlert is not null;

    [ObservableProperty]
    public partial string CountDisplay { get; set; } = string.Empty;

    [RelayCommand]
    private Task LoadAlertsAsync() => RunLoadAsync(async _ =>
    {
        var alerts = await stockAlertService.GetAllAsync();
        Alerts = new ObservableCollection<StockAlert>(alerts);

        var lowStock = await stockAlertService.GetLowStockProductsAsync();
        LowStockProducts = new ObservableCollection<Product>(lowStock);

        var overStock = await stockAlertService.GetOverStockProductsAsync();
        OverStockProducts = new ObservableCollection<Product>(overStock);

        CountDisplay = $"{alerts.Count} alerts · {lowStock.Count} low · {overStock.Count} over";
    });

    [RelayCommand]
    private async Task DeleteAlertAsync()
    {
        if (SelectedAlert is null) return;
        await stockAlertService.DeleteAsync(SelectedAlert.Id);
        await LoadAlertsAsync();
    }
}
