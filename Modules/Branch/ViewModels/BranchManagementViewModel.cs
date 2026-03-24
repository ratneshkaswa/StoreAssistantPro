using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Branch.Services;

namespace StoreAssistantPro.Modules.Branch.ViewModels;

public partial class BranchManagementViewModel(
    IBranchBillService branchService,
    IRegionalSettingsService regional) : BaseViewModel
{
    private List<BranchBill> _allItems = [];

    public string CurrencySymbol => regional.CurrencySymbol;

    [ObservableProperty]
    public partial ObservableCollection<BranchBill> Bills { get; set; } = [];

    [ObservableProperty]
    public partial int TotalBills { get; set; }

    [ObservableProperty]
    public partial int PendingBills { get; set; }

    [ObservableProperty]
    public partial decimal TotalAmount { get; set; }

    // ── Form fields ──

    [ObservableProperty]
    public partial DateTime BillDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial string BillNo { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AmountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedTypeIndex { get; set; }

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial string SaveButtonText { get; set; } = "Save";

    [ObservableProperty]
    public partial BranchBill? SelectedBill { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ActiveStatusFilter { get; set; } = "All";

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
            .Rule(!string.IsNullOrWhiteSpace(BillNo), "Bill number is required.", "BillNo")
            .Rule(!string.IsNullOrWhiteSpace(AmountText), "Amount is required.", "Amount")
            .Rule(decimal.TryParse(AmountText, out var amt) && amt > 0, "Enter a valid amount.", "Amount")))
            return;

        var amount = decimal.Parse(AmountText);
        var type = SelectedTypeIndex == 0 ? "Sent" : "Received";
        var dto = new BranchBillDto(BillDate, BillNo, amount, type);

        if (_editingId.HasValue)
        {
            await branchService.UpdateAsync(_editingId.Value, dto, ct);
            SuccessMessage = "Bill updated.";
        }
        else
        {
            await branchService.CreateAsync(dto, ct);
            SuccessMessage = "Bill added.";
        }

        ResetForm();
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private void Edit(BranchBill? item)
    {
        if (item is null) return;

        _editingId = item.Id;
        BillDate = item.Date;
        BillNo = item.BillNo;
        AmountText = item.Amount.ToString("F0");
        SelectedTypeIndex = item.Type == "Sent" ? 0 : 1;
        IsEditing = true;
        SaveButtonText = "Update";
    }

    [RelayCommand]
    private Task DeleteAsync(BranchBill? item) => RunAsync(async ct =>
    {
        if (item is null) return;
        await branchService.DeleteAsync(item.Id, ct);
        SuccessMessage = "Bill deleted.";
        ResetForm();
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task MarkClearedAsync(BranchBill? item) => RunAsync(async ct =>
    {
        if (item is null) return;
        await branchService.MarkClearedAsync(item.Id, ct);
        SuccessMessage = "Bill marked as cleared.";
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

    [RelayCommand]
    private Task ClearAllAsync() => RunAsync(async ct =>
    {
        var count = await branchService.ClearAllAsync(30, ct);
        SuccessMessage = count > 0 ? $"{count} cleared bills removed (30+ days old)." : "No bills older than 30 days to clear.";
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private void ExportCsv()
    {
        if (Bills.Count == 0) return;
        if (CsvExporter.Export(Bills, "BranchBills.csv"))
            SuccessMessage = "Exported to CSV.";
    }

    private void ResetForm()
    {
        _editingId = null;
        BillDate = DateTime.Today;
        BillNo = string.Empty;
        AmountText = string.Empty;
        SelectedTypeIndex = 0;
        IsEditing = false;
        SaveButtonText = "Save";
    }

    private void ApplyFilters()
    {
        IEnumerable<BranchBill> query = _allItems;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(b =>
                b.BillNo.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                b.Type.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (ActiveStatusFilter == "Pending")
            query = query.Where(b => !b.IsCleared);
        else if (ActiveStatusFilter == "Cleared")
            query = query.Where(b => b.IsCleared);

        var list = query.ToList();
        Bills = new ObservableCollection<BranchBill>(list);
        HasItems = list.Count > 0;
        FilterCountText = ActiveStatusFilter == "All" && string.IsNullOrWhiteSpace(SearchText) ? "" : $"{list.Count} bills";
    }

    private async Task ReloadAsync(CancellationToken ct)
    {
        var stats = await branchService.GetStatsAsync(ct);
        TotalBills = stats.TotalBills;
        PendingBills = stats.PendingBills;
        TotalAmount = stats.TotalAmount;

        var items = await branchService.GetAllAsync(ct: ct);
        _allItems = [.. items];
        ApplyFilters();
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
