using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Inventory.Services;
using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Modules.Inventory.ViewModels;

public partial class InventoryViewModel(
    IInventoryService inventoryService,
    IProductService productService,
    IRegionalSettingsService regional,
    IAppStateService appState) : BaseViewModel
{
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    //  Tab Navigation
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTabAdjust))]
    [NotifyPropertyChangedFor(nameof(IsTabLog))]
    [NotifyPropertyChangedFor(nameof(IsTabAlerts))]
    public partial int ActiveTab { get; set; }

    public bool IsTabAdjust => ActiveTab == 0;
    public bool IsTabLog => ActiveTab == 1;
    public bool IsTabAlerts => ActiveTab == 2;

    [RelayCommand]
    private void SwitchTab(string tab)
    {
        ActiveTab = int.TryParse(tab, out var t) ? t : 0;
        ClearMessages();
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    //  Tab 0 â€” Stock Adjustment
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    [ObservableProperty]
    public partial ObservableCollection<Product> Products { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedProduct))]
    public partial Product? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial string NewQuantity { get; set; } = string.Empty;

    [ObservableProperty]
    public partial AdjustmentReason SelectedReason { get; set; } = AdjustmentReason.Correction;

    [ObservableProperty]
    public partial string AdjustmentNotes { get; set; } = string.Empty;

    public ObservableCollection<AdjustmentReason> Reasons { get; } =
    [
        AdjustmentReason.Damage,
        AdjustmentReason.Theft,
        AdjustmentReason.Correction,
        AdjustmentReason.Return,
        AdjustmentReason.Transfer,
        AdjustmentReason.OpeningStock,
        AdjustmentReason.Other
    ];

    public bool HasSelectedProduct => SelectedProduct is not null;

    partial void OnSelectedProductChanged(Product? value)
    {
        AdjustCommand.NotifyCanExecuteChanged();

        if (value is not null)
            NewQuantity = value.Quantity.ToString();
    }

    [RelayCommand(CanExecute = nameof(CanAdjust))]
    private Task AdjustAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v
            .Rule(SelectedProduct is not null, "Select a product.")
            .Rule(int.TryParse(NewQuantity, out var q) && q >= 0, "Quantity must be a non-negative number.")))
            return;

        var selectedProductId = SelectedProduct!.Id;
        var userId = (int)appState.CurrentUserType;
        var dto = new StockAdjustmentDto(
            selectedProductId,
            null,
            int.Parse(NewQuantity),
            SelectedReason,
            string.IsNullOrWhiteSpace(AdjustmentNotes) ? null : AdjustmentNotes.Trim(),
            userId);

        await inventoryService.AdjustStockAsync(dto, ct);
        SuccessMessage = $"Stock adjusted: {SelectedProduct.Name} â†’ {NewQuantity}.";

        await Task.WhenAll(
            ReloadProductsAsync(ct),
            ReloadLogAsync(ct),
            ReloadAlertsAsync(ct));
        SelectedProduct = Products.FirstOrDefault(product => product.Id == selectedProductId);
        AdjustmentNotes = string.Empty;
    });

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    //  Tab 1 â€” Adjustment Log
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    [ObservableProperty]
    public partial ObservableCollection<StockAdjustment> AdjustmentLog { get; set; } = [];

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    //  Tab 2 â€” Alerts
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    [ObservableProperty]
    public partial ObservableCollection<Product> LowStockProducts { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Product> OutOfStockProducts { get; set; } = [];

    [ObservableProperty]
    public partial string TotalStockValue { get; set; } = string.Empty;

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    //  Load
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        await Task.WhenAll(
            ReloadProductsAsync(ct),
            ReloadLogAsync(ct),
            ReloadAlertsAsync(ct));
    });

    private async Task ReloadProductsAsync(CancellationToken ct)
    {
        var products = await productService.GetActiveAsync(ct);
        Products = new ObservableCollection<Product>(products);
    }

    private async Task ReloadLogAsync(CancellationToken ct)
    {
        var log = await inventoryService.GetRecentAdjustmentsAsync(100, ct);
        AdjustmentLog = new ObservableCollection<StockAdjustment>(log);
    }

    private async Task ReloadAlertsAsync(CancellationToken ct)
    {
        var lowTask = inventoryService.GetLowStockProductsAsync(ct);
        var oosTask = inventoryService.GetOutOfStockProductsAsync(ct);
        var valueTask = inventoryService.GetTotalStockValueAsync(ct);

        await Task.WhenAll(lowTask, oosTask, valueTask);

        LowStockProducts = new ObservableCollection<Product>(lowTask.Result);
        OutOfStockProducts = new ObservableCollection<Product>(oosTask.Result);
        TotalStockValue = regional.FormatCurrency(valueTask.Result);
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    private bool CanAdjust() => SelectedProduct is not null;
}


