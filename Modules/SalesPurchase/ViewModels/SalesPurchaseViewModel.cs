using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.SalesPurchase.Services;

namespace StoreAssistantPro.Modules.SalesPurchase.ViewModels;

public partial class SalesPurchaseViewModel(ISalesPurchaseService service) : BaseViewModel
{
    private List<SalesPurchaseEntry> _allItems = [];

    // ── Collections ──

    [ObservableProperty]
    public partial ObservableCollection<SalesPurchaseEntry> Entries { get; set; } = [];

    // ── Stat counters ──

    [ObservableProperty]
    public partial decimal TotalSales { get; set; }

    [ObservableProperty]
    public partial decimal TotalPurchases { get; set; }

    [ObservableProperty]
    public partial decimal NetBalance { get; set; }

    [ObservableProperty]
    public partial int EntryCount { get; set; }

    // ── Form fields ──

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AmountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime? EntryDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial int SelectedTypeIndex { get; set; }

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial string SaveButtonText { get; set; } = "Save";

    // ── Filters ──

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ActiveDateFilter { get; set; } = "All";

    [ObservableProperty]
    public partial string ActiveTypeFilter { get; set; } = "All";

    [ObservableProperty]
    public partial string FilterCountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasItems { get; set; } = true;

    // ── Selection ──

    [ObservableProperty]
    public partial SalesPurchaseEntry? SelectedEntry { get; set; }

    private int? _editingId;

    // ── Load ──

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        await ReloadAsync(ct);
    });

    // ── Search ──

    [RelayCommand]
    private void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            ApplyFilters();
            return;
        }
        var query = SearchText;
        var filtered = _allItems
            .Where(e => (e.Note ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                     || (e.Type ?? "").Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
        Entries = new ObservableCollection<SalesPurchaseEntry>(filtered);
        HasItems = filtered.Count > 0;
        FilterCountText = $"{filtered.Count} results";
    }

    // ── Filters ──

    [RelayCommand]
    private void SetDateFilter(string filter)
    {
        ActiveDateFilter = filter;
        ApplyFilters();
    }

    [RelayCommand]
    private void SetTypeFilter(string filter)
    {
        ActiveTypeFilter = filter;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var today = DateTime.Today;
        IEnumerable<SalesPurchaseEntry> filtered = _allItems;

        if (ActiveDateFilter != "All")
        {
            filtered = ActiveDateFilter switch
            {
                "Today" => filtered.Where(e => e.Date.Date == today),
                "Week" => filtered.Where(e => e.Date.Date >= today.AddDays(-(int)today.DayOfWeek)),
                "Month" => filtered.Where(e => e.Date.Year == today.Year && e.Date.Month == today.Month),
                _ => filtered
            };
        }

        if (ActiveTypeFilter != "All")
        {
            filtered = filtered.Where(e => e.Type == ActiveTypeFilter);
        }

        var list = filtered.ToList();
        Entries = new ObservableCollection<SalesPurchaseEntry>(list);
        HasItems = list.Count > 0;
        FilterCountText = (ActiveDateFilter == "All" && ActiveTypeFilter == "All") ? "" : $"{list.Count} entries";
    }

    // ── CRUD ──

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v
            .Rule(!string.IsNullOrWhiteSpace(Note), "Note / Bill No is required.", "Note")
            .Rule(!string.IsNullOrWhiteSpace(AmountText), "Amount is required.", "Amount")
            .Rule(decimal.TryParse(AmountText, out var amt) && amt > 0, "Amount must be a positive number.", "Amount")))
            return;

        var amount = decimal.Parse(AmountText);
        var type = SelectedTypeIndex == 0 ? "Sales" : "Purchase";
        var dto = new SalesPurchaseEntryDto(EntryDate ?? DateTime.Today, Note, amount, type);

        if (IsEditing && _editingId.HasValue)
        {
            await service.UpdateAsync(_editingId.Value, dto, ct);
            SuccessMessage = "Entry updated.";
        }
        else
        {
            await service.CreateAsync(dto, ct);
            SuccessMessage = "Entry added.";
        }

        ResetForm();
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private void Edit(SalesPurchaseEntry? entry)
    {
        if (entry is null) return;

        _editingId = entry.Id;
        Note = entry.Note;
        AmountText = entry.Amount.ToString("F2");
        EntryDate = entry.Date;
        SelectedTypeIndex = entry.Type == "Sales" ? 0 : 1;
        IsEditing = true;
        SaveButtonText = "Update";
    }

    [RelayCommand]
    private Task DeleteAsync(SalesPurchaseEntry? entry) => RunAsync(async ct =>
    {
        if (entry is null) return;

        await service.DeleteAsync(entry.Id, ct);
        SuccessMessage = "Entry deleted.";
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private void ClearForm() => ResetForm();

    [RelayCommand]
    private void ExportCsv()
    {
        if (Entries.Count == 0) return;
        if (CsvExporter.Export(Entries, "SalesPurchase.csv"))
            SuccessMessage = "Exported to CSV.";
    }

    [RelayCommand]
    private void CopyToClipboard(SalesPurchaseEntry? entry)
    {
        if (entry is null) return;
        Clipboard.SetText($"{entry.Date:d}\t{entry.Type}\t{entry.Note}\t{entry.Amount:N0}");
        SuccessMessage = "Copied to clipboard.";
    }

    private void ResetForm()
    {
        _editingId = null;
        Note = string.Empty;
        AmountText = string.Empty;
        EntryDate = DateTime.Today;
        SelectedTypeIndex = 0;
        IsEditing = false;
        SaveButtonText = "Save";
    }

    private async Task ReloadAsync(CancellationToken ct)
    {
        var stats = await service.GetStatsAsync(ct);
        TotalSales = stats.TotalSales;
        TotalPurchases = stats.TotalPurchases;
        NetBalance = stats.NetBalance;
        EntryCount = stats.EntryCount;

        var items = await service.GetAllAsync(ct: ct);
        _allItems = [.. items];
        ApplyFilters();
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
