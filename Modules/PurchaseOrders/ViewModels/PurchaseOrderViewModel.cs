using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.PurchaseOrders.Services;

namespace StoreAssistantPro.Modules.PurchaseOrders.ViewModels;

public partial class PurchaseOrderViewModel(
    IPurchaseOrderService poService,
    IProductService productService) : BaseViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<PurchaseOrder> Orders { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedOrder))]
    public partial PurchaseOrder? SelectedOrder { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Supplier> Suppliers { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Product> Products { get; set; } = [];

    public bool HasSelectedOrder => SelectedOrder is not null;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial PurchaseOrderStatus? FilterStatus { get; set; }

    public ObservableCollection<PurchaseOrderStatus?> StatusOptions { get; } =
    [
        null,
        PurchaseOrderStatus.Draft,
        PurchaseOrderStatus.Ordered,
        PurchaseOrderStatus.PartialReceived,
        PurchaseOrderStatus.Received,
        PurchaseOrderStatus.Cancelled
    ];

    // ── Paging ──

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    public partial int TotalPages { get; set; } = 1;

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    [ObservableProperty]
    public partial string PagingInfo { get; set; } = string.Empty;

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    private const int PageSize = 25;

    [ObservableProperty]
    public partial Supplier? SelectedSupplier { get; set; }

    [ObservableProperty]
    public partial DateTime? ExpectedDate { get; set; }

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<PurchaseOrderLineInput> LineItems { get; set; } = [];

    [ObservableProperty]
    public partial PurchaseOrderLineInput? SelectedLineItem { get; set; }

    partial void OnSelectedOrderChanged(PurchaseOrder? value)
    {
        MarkOrderedCommand.NotifyCanExecuteChanged();
        CancelOrderCommand.NotifyCanExecuteChanged();
        ReceiveAllCommand.NotifyCanExecuteChanged();
    }

    partial void OnLineItemsChanged(ObservableCollection<PurchaseOrderLineInput> value)
    {
        AttachLineItems(value);
        SelectedLineItem ??= value.FirstOrDefault();
        UpdateLineItemCommandStates();
    }

    partial void OnSelectedLineItemChanged(PurchaseOrderLineInput? value) => UpdateLineItemCommandStates();

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        var suppliersTask = poService.GetActiveSuppliersAsync(ct);
        var productsTask = productService.GetActiveAsync(ct);

        await Task.WhenAll(suppliersTask, productsTask);

        Suppliers = new ObservableCollection<Supplier>(suppliersTask.Result);
        Products = new ObservableCollection<Product>(productsTask.Result);

        await ReloadOrdersAsync(ct);
        EnsureSeedLine();
    });

    [RelayCommand]
    private Task SearchAsync() => RunAsync(async ct =>
    {
        CurrentPage = 1;
        await ReloadOrdersAsync(ct);
    });

    [RelayCommand]
    private Task PreviousPageAsync() => RunAsync(async ct =>
    {
        if (!HasPreviousPage) return;
        CurrentPage--;
        await ReloadOrdersAsync(ct);
    });

    [RelayCommand]
    private Task NextPageAsync() => RunAsync(async ct =>
    {
        if (!HasNextPage) return;
        CurrentPage++;
        await ReloadOrdersAsync(ct);
    });

    private async Task ReloadOrdersAsync(CancellationToken ct, int? selectedOrderId = null)
    {
        var search = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery;
        var result = await poService.GetPagedAsync(new PagedQuery(CurrentPage, PageSize), search, FilterStatus, null, null, ct);
        Orders = new ObservableCollection<PurchaseOrder>(result.Items);
        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages == 0 ? 1 : result.TotalPages;
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
        PagingInfo = TotalCount > 0
            ? $"Page {CurrentPage} of {TotalPages} ({TotalCount} total)"
            : string.Empty;
        SelectedOrder = selectedOrderId.HasValue
            ? Orders.FirstOrDefault(o => o.Id == selectedOrderId.Value)
            : null;
    }

    [RelayCommand]
    private Task CreateOrderAsync() => RunAsync(async ct =>
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        var enteredItems = LineItems
            .Where(line => line.ProductId > 0 || line.Quantity > 0 || line.UnitCost > 0)
            .ToList();

        if (!Validate(v => v
            .Rule(SelectedSupplier is not null, "Select a supplier.")
            .Rule(enteredItems.Count > 0, "Add at least one item.")))
        {
            return;
        }

        var invalidLine = enteredItems
            .Select((line, index) => new { line, index })
            .FirstOrDefault(x => x.line.ProductId <= 0 || x.line.Quantity <= 0 || x.line.UnitCost <= 0);

        if (invalidLine is not null)
        {
            ErrorMessage = $"Complete line {invalidLine.index + 1} with a product, quantity, and unit cost.";
            return;
        }

        var dto = new CreatePurchaseOrderDto(
            SelectedSupplier!.Id,
            ExpectedDate,
            string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            enteredItems.Select(line => new PurchaseOrderLineDto(line.ProductId, line.Quantity, line.UnitCost)).ToList());

        var po = await poService.CreateAsync(dto, ct);
        SuccessMessage = $"PO {po.OrderNumber} created.";

        SelectedSupplier = null;
        ExpectedDate = null;
        Notes = string.Empty;
        LineItems.Clear();
        EnsureSeedLine();

        await ReloadOrdersAsync(ct);
    });

    [RelayCommand]
    private void AddLineItem()
    {
        var line = new PurchaseOrderLineInput
        {
            Owner = this
        };
        LineItems.Add(line);
        SelectedLineItem = line;
        UpdateLineItemCommandStates();
    }

    [RelayCommand(CanExecute = nameof(CanRemoveLineItem))]
    private void RemoveLineItem()
    {
        if (SelectedLineItem is null)
        {
            return;
        }

        LineItems.Remove(SelectedLineItem);

        if (LineItems.Count == 0)
        {
            EnsureSeedLine();
            return;
        }

        SelectedLineItem = LineItems.FirstOrDefault();
        UpdateLineItemCommandStates();
    }

    [RelayCommand(CanExecute = nameof(CanMarkOrdered))]
    private Task MarkOrderedAsync() => RunAsync(async ct =>
    {
        if (SelectedOrder is null)
        {
            return;
        }

        var orderId = SelectedOrder.Id;
        var orderNumber = SelectedOrder.OrderNumber;

        await poService.UpdateStatusAsync(orderId, PurchaseOrderStatus.Ordered, ct);
        SuccessMessage = $"PO {orderNumber} marked as Ordered.";

        await ReloadOrdersAsync(ct, orderId);
    });

    [RelayCommand(CanExecute = nameof(CanCancelOrder))]
    private Task CancelOrderAsync() => RunAsync(async ct =>
    {
        if (SelectedOrder is null)
        {
            return;
        }

        var orderId = SelectedOrder.Id;
        var orderNumber = SelectedOrder.OrderNumber;

        await poService.UpdateStatusAsync(orderId, PurchaseOrderStatus.Cancelled, ct);
        SuccessMessage = $"PO {orderNumber} cancelled.";

        await ReloadOrdersAsync(ct, orderId);
    });

    [RelayCommand(CanExecute = nameof(CanReceiveAll))]
    private Task ReceiveAllAsync() => RunAsync(async ct =>
    {
        if (SelectedOrder is null)
        {
            return;
        }

        ErrorMessage = string.Empty;
        var orderId = SelectedOrder.Id;
        var orderNumber = SelectedOrder.OrderNumber;

        var lines = SelectedOrder.Items
            .Where(item => item.QuantityReceived < item.Quantity)
            .Select(item => new ReceiveLineDto(item.Id, item.Quantity - item.QuantityReceived))
            .ToList();

        if (lines.Count == 0)
        {
            ErrorMessage = "All items already received.";
            return;
        }

        await poService.ReceiveItemsAsync(orderId, lines, ct);
        SuccessMessage = $"PO {orderNumber} - all items received. Stock updated.";

        await ReloadOrdersAsync(ct, orderId);
    });

    private void EnsureSeedLine()
    {
        if (LineItems.Count > 0)
        {
            AttachLineItems(LineItems);
            SelectedLineItem ??= LineItems.FirstOrDefault();
            UpdateLineItemCommandStates();
            return;
        }

        var line = new PurchaseOrderLineInput
        {
            Owner = this
        };
        LineItems.Add(line);
        SelectedLineItem = line;
        UpdateLineItemCommandStates();
    }

    private bool CanRemoveLineItem()
    {
        if (SelectedLineItem is null)
        {
            return false;
        }

        return LineItems.Count > 1 || IsLineEntered(SelectedLineItem);
    }

    private bool CanMarkOrdered() =>
        SelectedOrder?.Status == PurchaseOrderStatus.Draft;

    private bool CanCancelOrder() =>
        SelectedOrder is { Status: not PurchaseOrderStatus.Cancelled and not PurchaseOrderStatus.Received };

    private bool CanReceiveAll() =>
        SelectedOrder is { Status: PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartialReceived }
        && SelectedOrder.Items.Any(item => item.QuantityReceived < item.Quantity);

    private void UpdateLineItemCommandStates() =>
        RemoveLineItemCommand.NotifyCanExecuteChanged();

    private void AttachLineItems(IEnumerable<PurchaseOrderLineInput> items)
    {
        foreach (var line in items)
        {
            line.Owner = this;
        }
    }

    private static bool IsLineEntered(PurchaseOrderLineInput line) =>
        line.ProductId > 0 || line.Quantity > 0 || line.UnitCost > 0;
}

public partial class PurchaseOrderLineInput : ObservableObject
{
    internal PurchaseOrderViewModel? Owner { get; set; }

    [ObservableProperty]
    public partial int ProductId { get; set; }

    [ObservableProperty]
    public partial int Quantity { get; set; }

    [ObservableProperty]
    public partial decimal UnitCost { get; set; }

    partial void OnProductIdChanged(int value) => Owner?.RemoveLineItemCommand.NotifyCanExecuteChanged();

    partial void OnQuantityChanged(int value) => Owner?.RemoveLineItemCommand.NotifyCanExecuteChanged();

    partial void OnUnitCostChanged(decimal value) => Owner?.RemoveLineItemCommand.NotifyCanExecuteChanged();
}

