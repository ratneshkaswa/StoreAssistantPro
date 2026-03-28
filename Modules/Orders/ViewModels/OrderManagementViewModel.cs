using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Orders.Services;

namespace StoreAssistantPro.Modules.Orders.ViewModels;

public partial class OrderManagementViewModel(
    IOrderService orderService,
    IRegionalSettingsService regional) : BaseViewModel
{

    private static readonly TimeSpan NavigationFreshnessWindow = TimeSpan.FromMinutes(2);
    private List<Order> _allItems = [];
    private bool _isRestoringViewState;

    public string CurrencySymbol => regional.CurrencySymbol;

    [ObservableProperty]
    public partial ObservableCollection<Order> Orders { get; set; } = [];

    [ObservableProperty]
    public partial int PendingCount { get; set; }

    [ObservableProperty]
    public partial int ActiveCount { get; set; }

    [ObservableProperty]
    public partial int DeliveredCount { get; set; }

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    // ── Form fields ──

    [ObservableProperty]
    public partial DateTime OrderDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial string CustomerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ItemDescription { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string QuantityText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RateText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime? DeliveryDate { get; set; }

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial string SaveButtonText { get; set; } = "Save";

    [ObservableProperty]
    public partial Order? SelectedOrder { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    partial void OnSearchTextChanged(string value) => PersistViewState();

    [ObservableProperty]
    public partial string ActiveStatusFilter { get; set; } = "All";

    partial void OnActiveStatusFilterChanged(string value) => PersistViewState();

    [ObservableProperty]
    public partial string FilterCountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasItems { get; set; } = true;

    private int? _editingId;

    [RelayCommand]
    private Task LoadAsync() => LoadOnActivateAsync(async ct =>
    {
        RestoreViewState();
        await ReloadAsync(ct);
    },
        NavigationFreshnessWindow);

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v
            .Rule(!string.IsNullOrWhiteSpace(CustomerName), "Customer name is required.", "Customer")))
            return;

        int.TryParse(QuantityText, out var qty);
        decimal.TryParse(RateText, out var rate);
        var amount = qty * rate;

        var dto = new OrderDto(OrderDate, CustomerName, ItemDescription, qty, rate, amount, DeliveryDate);

        if (_editingId.HasValue)
        {
            await orderService.UpdateAsync(_editingId.Value, dto, ct);
            SuccessMessage = "Order updated.";
        }
        else
        {
            await orderService.CreateAsync(dto, ct);
            SuccessMessage = "Order added.";
        }

        ResetForm();
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private void Edit(Order? item)
    {
        if (item is null) return;

        _editingId = item.Id;
        OrderDate = item.Date;
        CustomerName = item.CustomerName;
        ItemDescription = item.ItemDescription;
        QuantityText = item.Quantity.ToString();
        RateText = item.Rate.ToString("F0");
        DeliveryDate = item.DeliveryDate;
        IsEditing = true;
        SaveButtonText = "Update";
    }

    [RelayCommand]
    private Task DeleteAsync(Order? item) => RunAsync(async ct =>
    {
        if (item is null) return;
        await orderService.DeleteAsync(item.Id, ct);
        SuccessMessage = "Order deleted.";
        ResetForm();
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task MarkDeliveredAsync(Order? item) => SetStatusAsync(item, "Delivered");

    [RelayCommand]
    private Task MarkPendingAsync(Order? item) => SetStatusAsync(item, "Pending");

    [RelayCommand]
    private Task ConfirmOrderAsync(Order? item) => SetStatusAsync(item, "Confirmed");

    [RelayCommand]
    private Task MarkReadyAsync(Order? item) => SetStatusAsync(item, "Ready");

    [RelayCommand]
    private Task CancelOrderAsync(Order? item) => SetStatusAsync(item, "Cancelled");

    [RelayCommand]
    private void ExportCsv()
    {
        if (Orders.Count == 0) return;
        if (CsvExporter.Export(Orders, "Orders.csv"))
            SuccessMessage = "Exported to CSV.";
    }

    private Task SetStatusAsync(Order? item, string status) => RunAsync(async ct =>
    {
        if (item is null) return;
        await orderService.SetStatusAsync(item.Id, status, ct);
        SuccessMessage = $"Order marked as {status}.";
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private void ClearForm() => ResetForm();

    [RelayCommand]
    private void Search() => ApplyFilters();

    [RelayCommand]
    private void SetStatusFilter(string filter)
    {
        ActiveStatusFilter = filter;
        ApplyFilters();
    }

    private void ResetForm()
    {
        _editingId = null;
        OrderDate = DateTime.Today;
        CustomerName = string.Empty;
        ItemDescription = string.Empty;
        QuantityText = string.Empty;
        RateText = string.Empty;
        DeliveryDate = null;
        IsEditing = false;
        SaveButtonText = "Save";
    }

    private void ApplyFilters()
    {
        IEnumerable<Order> query = _allItems;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(o =>
                o.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                o.ItemDescription.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (ActiveStatusFilter != "All")
        {
            query = ActiveStatusFilter == "Active"
                ? query.Where(o => o.Status != "Delivered" && o.Status != "Cancelled")
                : query.Where(o => o.Status == ActiveStatusFilter);
        }

        var list = query.ToList();
        Orders = new ObservableCollection<Order>(list);
        HasItems = list.Count > 0;
        FilterCountText = ActiveStatusFilter == "All" && string.IsNullOrWhiteSpace(SearchText) ? "" : $"{list.Count} orders";
    }

    private async Task ReloadAsync(CancellationToken ct)
    {
        var stats = await orderService.GetStatsAsync(ct);
        PendingCount = stats.Pending;
        ActiveCount = stats.Active;
        DeliveredCount = stats.Delivered;
        TotalCount = stats.Total;

        var items = await orderService.GetAllAsync(ct);
        _allItems = [.. items];
        ApplyFilters();
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    private void RestoreViewState()
    {
        _isRestoringViewState = true;
        try
        {
            var state = UserPreferencesStore.GetOrderManagementState();
            SearchText = state.SearchText;
            ActiveStatusFilter = state.ActiveFilter;
        }
        finally
        {
            _isRestoringViewState = false;
        }
    }

    private void PersistViewState()
    {
        if (_isRestoringViewState)
            return;

        UserPreferencesStore.SetOrderManagementState(new SearchFilterViewState
        {
            SearchText = SearchText,
            ActiveFilter = ActiveStatusFilter
        });
    }
}
