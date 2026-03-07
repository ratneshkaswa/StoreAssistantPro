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
    // ═══════════════════════════════════════════════════════════════
    //  Tab Navigation
    // ═══════════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════════
    //  Tab 0 — Stock Adjustment
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial ObservableCollection<Product> Products { get; set; } = [];

    [ObservableProperty]
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

    partial void OnSelectedProductChanged(Product? value)
    {
        if (value is not null)
            NewQuantity = value.Quantity.ToString();
    }

    [RelayCommand]
    private Task AdjustAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v
            .Rule(SelectedProduct is not null, "Select a product.")
            .Rule(int.TryParse(NewQuantity, out var q) && q >= 0, "Quantity must be a non-negative number.")))
            return;

        var userId = (int)appState.CurrentUserType;
        var dto = new StockAdjustmentDto(
            SelectedProduct!.Id,
            null,
            int.Parse(NewQuantity),
            SelectedReason,
            string.IsNullOrWhiteSpace(AdjustmentNotes) ? null : AdjustmentNotes.Trim(),
            userId);

        await inventoryService.AdjustStockAsync(dto, ct);
        SuccessMessage = $"Stock adjusted: {SelectedProduct.Name} → {NewQuantity}.";

        await ReloadProductsAsync(ct);
        await ReloadLogAsync(ct);
    });

    // ═══════════════════════════════════════════════════════════════
    //  Tab 1 — Adjustment Log
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial ObservableCollection<StockAdjustment> AdjustmentLog { get; set; } = [];

    // ═══════════════════════════════════════════════════════════════
    //  Tab 2 — Alerts
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial ObservableCollection<Product> LowStockProducts { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Product> OutOfStockProducts { get; set; } = [];

    [ObservableProperty]
    public partial string TotalStockValue { get; set; } = string.Empty;

    // ═══════════════════════════════════════════════════════════════
    //  Load
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        await ReloadProductsAsync(ct);
        await ReloadLogAsync(ct);
        await ReloadAlertsAsync(ct);
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
        var low = await inventoryService.GetLowStockProductsAsync(ct);
        LowStockProducts = new ObservableCollection<Product>(low);

        var oos = await inventoryService.GetOutOfStockProductsAsync(ct);
        OutOfStockProducts = new ObservableCollection<Product>(oos);

        var value = await inventoryService.GetTotalStockValueAsync(ct);
        TotalStockValue = regional.FormatCurrency(value);
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
