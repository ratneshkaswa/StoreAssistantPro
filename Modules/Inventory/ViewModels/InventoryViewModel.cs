using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Inventory.Services;
using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Modules.Inventory.ViewModels;

public partial class InventoryViewModel(
    IInventoryService inventoryService,
    IProductService productService,
    IStockTakeService stockTakeService,
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
    [NotifyPropertyChangedFor(nameof(IsTabStockTake))]
    public partial int ActiveTab { get; set; }

    public bool IsTabAdjust => ActiveTab == 0;
    public bool IsTabLog => ActiveTab == 1;
    public bool IsTabAlerts => ActiveTab == 2;
    public bool IsTabStockTake => ActiveTab == 3;

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
    public partial ObservableCollection<ProductVariant> LowStockVariants { get; set; } = [];

    [ObservableProperty]
    public partial string TotalStockValue { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<Product> DeadStockProducts { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<StockMovementEntry> StockMovementHistory { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMovementHistory))]
    public partial bool ShowMovementHistory { get; set; }

    public bool HasMovementHistory => ShowMovementHistory && StockMovementHistory.Count > 0;

    // ═══════════════════════════════════════════════════════════════
    //  Tab 3 — Stock Take (#69)
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveStockTake))]
    [NotifyPropertyChangedFor(nameof(NoActiveStockTake))]
    public partial StockTake? ActiveStockTake { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<StockTakeItemVm> StockTakeItems { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<StockTake> RecentStockTakes { get; set; } = [];

    [ObservableProperty]
    public partial string StockTakeNotes { get; set; } = string.Empty;

    public bool HasActiveStockTake => ActiveStockTake is not null;
    public bool NoActiveStockTake => ActiveStockTake is null;

    [RelayCommand]
    private Task StartStockTakeAsync() => RunAsync(async ct =>
    {
        ClearMessages();
        var userId = (int)appState.CurrentUserType;
        var stockTake = await stockTakeService.StartAsync(
            string.IsNullOrWhiteSpace(StockTakeNotes) ? null : StockTakeNotes, userId, ct);

        ActiveStockTake = stockTake;
        StockTakeItems = new ObservableCollection<StockTakeItemVm>(
            stockTake.Items.Select(i => new StockTakeItemVm(i)));
        StockTakeNotes = string.Empty;

        SuccessMessage = $"Stock take {stockTake.Reference} started with {stockTake.TotalItems} items.";
    });

    [RelayCommand]
    private Task LoadStockTakeAsync(StockTake? st) => RunAsync(async ct =>
    {
        if (st is null) return;
        ClearMessages();
        var stockTake = await stockTakeService.GetByIdAsync(st.Id, ct);
        if (stockTake is null) { ErrorMessage = "Stock take not found."; return; }

        ActiveStockTake = stockTake;
        StockTakeItems = new ObservableCollection<StockTakeItemVm>(
            stockTake.Items.Select(i => new StockTakeItemVm(i)));
    });

    [RelayCommand]
    private Task SaveCountAsync(StockTakeItemVm? item) => RunAsync(async ct =>
    {
        if (item is null || ActiveStockTake is null) return;

        if (!int.TryParse(item.CountedText, out var counted) || counted < 0)
        {
            ErrorMessage = "Enter a valid non-negative count.";
            return;
        }

        await stockTakeService.UpdateCountAsync(item.ItemId, counted, ct);
        item.IsCounted = true;
        item.CountedQuantity = counted;
        ErrorMessage = string.Empty;
    });

    [RelayCommand]
    private Task CompleteStockTakeAsync() => RunAsync(async ct =>
    {
        if (ActiveStockTake is null) return;
        ClearMessages();

        var uncounted = StockTakeItems.Count(i => !i.IsCounted);
        if (uncounted > 0)
        {
            ErrorMessage = $"{uncounted} item(s) not yet counted. Count all items or cancel.";
            return;
        }

        var userId = (int)appState.CurrentUserType;
        var result = await stockTakeService.CompleteAsync(ActiveStockTake.Id, userId, ct);

        ActiveStockTake = null;
        StockTakeItems = [];
        await ReloadStockTakesAsync(ct);
        await Task.WhenAll(ReloadProductsAsync(ct), ReloadLogAsync(ct), ReloadAlertsAsync(ct));

        SuccessMessage = $"Stock take complete: {result.TotalItems} items, {result.Discrepancies} discrepancies, {result.Adjusted} adjusted.";
    });

    [RelayCommand]
    private Task CancelStockTakeAsync() => RunAsync(async ct =>
    {
        if (ActiveStockTake is null) return;
        ClearMessages();

        await stockTakeService.CancelAsync(ActiveStockTake.Id, ct);
        ActiveStockTake = null;
        StockTakeItems = [];
        await ReloadStockTakesAsync(ct);
        SuccessMessage = "Stock take cancelled.";
    });

    private async Task ReloadStockTakesAsync(CancellationToken ct)
    {
        var recent = await stockTakeService.GetRecentAsync(20, ct);
        RecentStockTakes = new ObservableCollection<StockTake>(recent);
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    //  Load
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        await Task.WhenAll(
            ReloadProductsAsync(ct),
            ReloadLogAsync(ct),
            ReloadAlertsAsync(ct),
            ReloadStockTakesAsync(ct));
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
        var variantTask = inventoryService.GetLowStockVariantsAsync(ct);
        var valueTask = inventoryService.GetTotalStockValueAsync(ct);
        var deadTask = inventoryService.GetDeadStockAsync(90, ct);

        await Task.WhenAll(lowTask, oosTask, variantTask, valueTask, deadTask);

        LowStockProducts = new ObservableCollection<Product>(lowTask.Result);
        OutOfStockProducts = new ObservableCollection<Product>(oosTask.Result);
        LowStockVariants = new ObservableCollection<ProductVariant>(variantTask.Result);
        TotalStockValue = regional.FormatCurrency(valueTask.Result);
        DeadStockProducts = new ObservableCollection<Product>(deadTask.Result);
    }

    [RelayCommand]
    private void ExportCsv()
    {
        if (Products.Count == 0) return;
        if (CsvExporter.Export(Products, "Inventory.csv"))
            SuccessMessage = "Exported to CSV.";
    }

    [RelayCommand]
    private void ExportLogCsv()
    {
        if (AdjustmentLog.Count == 0) return;
        if (CsvExporter.Export(AdjustmentLog, "AdjustmentLog.csv"))
            SuccessMessage = "Exported to CSV.";
    }

    [RelayCommand]
    private Task ImportCsvAsync() => RunAsync(async ct =>
    {
        ClearMessages();
        var rows = CsvImporter.Import();
        if (rows is null) return;
        if (rows.Count == 0) { ErrorMessage = "CSV file is empty."; return; }

        var userId = (int)appState.CurrentUserType;
        var imported = await inventoryService.ImportStockAsync(rows, userId, ct);
        SuccessMessage = $"Updated stock for {imported} product(s).";

        await Task.WhenAll(
            ReloadProductsAsync(ct),
            ReloadLogAsync(ct),
            ReloadAlertsAsync(ct));
    });

    [RelayCommand]
    private Task LoadMovementHistoryAsync(Product? product) => RunAsync(async ct =>
    {
        if (product is null) return;
        var history = await inventoryService.GetStockMovementHistoryAsync(product.Id, ct);
        StockMovementHistory = new ObservableCollection<StockMovementEntry>(history);
        ShowMovementHistory = true;
        OnPropertyChanged(nameof(HasMovementHistory));
    });

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    private bool CanAdjust() => SelectedProduct is not null;
}
