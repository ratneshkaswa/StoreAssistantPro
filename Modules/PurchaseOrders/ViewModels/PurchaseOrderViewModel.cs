using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.PurchaseOrders.Services;

namespace StoreAssistantPro.Modules.PurchaseOrders.ViewModels;

public partial class PurchaseOrderViewModel(
    IPurchaseOrderService poService) : BaseViewModel
{
    // ── Data ──

    [ObservableProperty]
    public partial ObservableCollection<PurchaseOrder> Orders { get; set; } = [];

    [ObservableProperty]
    public partial PurchaseOrder? SelectedOrder { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Supplier> Suppliers { get; set; } = [];

    // ── Filters ──

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial PurchaseOrderStatus? FilterStatus { get; set; }

    public ObservableCollection<PurchaseOrderStatus?> StatusOptions { get; } =
    [
        null, // All
        PurchaseOrderStatus.Draft,
        PurchaseOrderStatus.Ordered,
        PurchaseOrderStatus.PartialReceived,
        PurchaseOrderStatus.Received,
        PurchaseOrderStatus.Cancelled
    ];

    // ── New PO form ──

    [ObservableProperty]
    public partial Supplier? SelectedSupplier { get; set; }

    [ObservableProperty]
    public partial DateTime? ExpectedDate { get; set; }

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<PurchaseOrderLineInput> LineItems { get; set; } = [];

    // ── Commands ──

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        var orders = await poService.GetAllAsync(ct);
        Orders = new ObservableCollection<PurchaseOrder>(orders);

        var suppliers = await poService.GetActiveSuppliersAsync(ct);
        Suppliers = new ObservableCollection<Supplier>(suppliers);
    });

    [RelayCommand]
    private Task SearchAsync() => RunAsync(async ct =>
    {
        var results = await poService.SearchAsync(SearchQuery, FilterStatus, null, null, ct);
        Orders = new ObservableCollection<PurchaseOrder>(results);
    });

    [RelayCommand]
    private Task CreateOrderAsync() => RunAsync(async ct =>
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (!Validate(v => v
            .Rule(SelectedSupplier is not null, "Select a supplier.")
            .Rule(LineItems.Count > 0, "Add at least one item.")))
            return;

        var dto = new CreatePurchaseOrderDto(
            SelectedSupplier!.Id,
            ExpectedDate,
            string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            LineItems.Select(l => new PurchaseOrderLineDto(l.ProductId, l.Quantity, l.UnitCost)).ToList());

        var po = await poService.CreateAsync(dto, ct);
        SuccessMessage = $"PO {po.OrderNumber} created.";

        LineItems.Clear();
        Notes = string.Empty;
        ExpectedDate = null;

        var orders = await poService.GetAllAsync(ct);
        Orders = new ObservableCollection<PurchaseOrder>(orders);
    });

    [RelayCommand]
    private Task MarkOrderedAsync() => RunAsync(async ct =>
    {
        if (SelectedOrder is null) return;
        await poService.UpdateStatusAsync(SelectedOrder.Id, PurchaseOrderStatus.Ordered, ct);
        SuccessMessage = $"PO {SelectedOrder.OrderNumber} marked as Ordered.";
        var orders = await poService.GetAllAsync(ct);
        Orders = new ObservableCollection<PurchaseOrder>(orders);
    });

    [RelayCommand]
    private Task CancelOrderAsync() => RunAsync(async ct =>
    {
        if (SelectedOrder is null) return;
        await poService.UpdateStatusAsync(SelectedOrder.Id, PurchaseOrderStatus.Cancelled, ct);
        SuccessMessage = $"PO {SelectedOrder.OrderNumber} cancelled.";
        var orders = await poService.GetAllAsync(ct);
        Orders = new ObservableCollection<PurchaseOrder>(orders);
    });

    [RelayCommand]
    private Task ReceiveAllAsync() => RunAsync(async ct =>
    {
        if (SelectedOrder is null) return;
        ErrorMessage = string.Empty;

        var lines = SelectedOrder.Items
            .Where(i => i.QuantityReceived < i.Quantity)
            .Select(i => new ReceiveLineDto(i.Id, i.Quantity - i.QuantityReceived))
            .ToList();

        if (lines.Count == 0)
        {
            ErrorMessage = "All items already received.";
            return;
        }

        await poService.ReceiveItemsAsync(SelectedOrder.Id, lines, ct);
        SuccessMessage = $"PO {SelectedOrder.OrderNumber} — all items received. Stock updated.";

        var orders = await poService.GetAllAsync(ct);
        Orders = new ObservableCollection<PurchaseOrder>(orders);
    });
}

/// <summary>Line item input for creating a new PO.</summary>
public partial class PurchaseOrderLineInput : ObservableObject
{
    public int ProductId { get; set; }

    [ObservableProperty]
    public partial string ProductName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int Quantity { get; set; } = 1;

    [ObservableProperty]
    public partial decimal UnitCost { get; set; }
}
