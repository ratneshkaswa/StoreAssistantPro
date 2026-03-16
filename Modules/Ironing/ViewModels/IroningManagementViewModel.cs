using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Ironing.Services;

namespace StoreAssistantPro.Modules.Ironing.ViewModels;

public partial class IroningManagementViewModel(IIroningService ironingService) : BaseViewModel
{
    private List<IroningEntry> _allItems = [];

    [ObservableProperty]
    public partial ObservableCollection<IroningEntry> Entries { get; set; } = [];

    [ObservableProperty]
    public partial int TotalEntries { get; set; }

    [ObservableProperty]
    public partial int UnpaidEntries { get; set; }

    [ObservableProperty]
    public partial decimal TotalAmount { get; set; }

    [ObservableProperty]
    public partial int ActiveBatches { get; set; }

    // ── Form fields ──

    [ObservableProperty]
    public partial DateTime EntryDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial string CustomerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Items { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string QuantityText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RateText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial string SaveButtonText { get; set; } = "Save";

    [ObservableProperty]
    public partial IroningEntry? SelectedEntry { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ActivePaidFilter { get; set; } = "All";

    [ObservableProperty]
    public partial string FilterCountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasItems { get; set; } = true;

    private int? _editingId;

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v
            .Rule(!string.IsNullOrWhiteSpace(CustomerName), "Customer name is required.", "Customer")
            .Rule(!string.IsNullOrWhiteSpace(QuantityText), "Quantity is required.", "Quantity")
            .Rule(int.TryParse(QuantityText, out var q) && q > 0, "Enter a valid quantity.", "Quantity")))
            return;

        var qty = int.Parse(QuantityText);
        decimal.TryParse(RateText, out var rate);
        var amount = qty * rate;
        var dto = new IroningEntryDto(EntryDate, CustomerName, Items, qty, rate, amount, false);

        if (_editingId.HasValue)
        {
            await ironingService.UpdateEntryAsync(_editingId.Value, dto, ct);
            SuccessMessage = "Entry updated.";
        }
        else
        {
            await ironingService.CreateEntryAsync(dto, ct);
            SuccessMessage = "Entry added.";
        }

        ResetForm();
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private void Edit(IroningEntry? item)
    {
        if (item is null) return;

        _editingId = item.Id;
        EntryDate = item.Date;
        CustomerName = item.CustomerName;
        Items = item.Items;
        QuantityText = item.Quantity.ToString();
        RateText = item.Rate.ToString("F0");
        IsEditing = true;
        SaveButtonText = "Update";
    }

    [RelayCommand]
    private Task DeleteAsync(IroningEntry? item) => RunAsync(async ct =>
    {
        if (item is null) return;
        await ironingService.DeleteEntryAsync(item.Id, ct);
        SuccessMessage = "Entry deleted.";
        ResetForm();
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task MarkPaidAsync(IroningEntry? item) => RunAsync(async ct =>
    {
        if (item is null) return;
        await ironingService.MarkPaidAsync(item.Id, ct);
        SuccessMessage = "Marked as paid.";
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private void ClearForm() => ResetForm();

    [RelayCommand]
    private void Search() => ApplyFilters();

    [RelayCommand]
    private void SetPaidFilter(string filter)
    {
        ActivePaidFilter = filter;
        ApplyFilters();
    }

    [RelayCommand]
    private void ExportCsv()
    {
        if (Entries.Count == 0) return;
        if (CsvExporter.Export(Entries, "Ironing.csv"))
            SuccessMessage = "Exported to CSV.";
    }

    private void ResetForm()
    {
        _editingId = null;
        EntryDate = DateTime.Today;
        CustomerName = string.Empty;
        Items = string.Empty;
        QuantityText = string.Empty;
        RateText = string.Empty;
        IsEditing = false;
        SaveButtonText = "Save";
    }

    private void ApplyFilters()
    {
        IEnumerable<IroningEntry> query = _allItems;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(e =>
                e.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Items.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (ActivePaidFilter == "Unpaid")
            query = query.Where(e => !e.IsPaid);
        else if (ActivePaidFilter == "Paid")
            query = query.Where(e => e.IsPaid);

        var list = query.ToList();
        Entries = new ObservableCollection<IroningEntry>(list);
        HasItems = list.Count > 0;
        FilterCountText = ActivePaidFilter == "All" && string.IsNullOrWhiteSpace(SearchText) ? "" : $"{list.Count} entries";
    }

    private async Task ReloadAsync(CancellationToken ct)
    {
        var stats = await ironingService.GetStatsAsync(ct);
        TotalEntries = stats.TotalEntries;
        UnpaidEntries = stats.UnpaidEntries;
        TotalAmount = stats.TotalAmount;
        ActiveBatches = stats.ActiveBatches;

        var entries = await ironingService.GetAllEntriesAsync(ct);
        _allItems = [.. entries];
        ApplyFilters();
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
