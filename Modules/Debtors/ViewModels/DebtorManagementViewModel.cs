using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Debtors.Services;

namespace StoreAssistantPro.Modules.Debtors.ViewModels;

public partial class DebtorManagementViewModel(
    IDebtorService debtorService,
    IRegionalSettingsService regional) : BaseViewModel
{
    private List<Debtor> _allItems = [];

    public string CurrencySymbol => regional.CurrencySymbol;

    [ObservableProperty]
    public partial ObservableCollection<Debtor> Debtors { get; set; } = [];

    [ObservableProperty]
    public partial int TotalDebtors { get; set; }

    [ObservableProperty]
    public partial int PendingDebtors { get; set; }

    [ObservableProperty]
    public partial decimal TotalOutstanding { get; set; }

    // ── Form fields ──

    [ObservableProperty]
    public partial string DebtorName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Phone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TotalAmountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PaidAmountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime DebtDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial string SaveButtonText { get; set; } = "Save";

    [ObservableProperty]
    public partial Debtor? SelectedDebtor { get; set; }

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
            .Rule(!string.IsNullOrWhiteSpace(DebtorName), "Name is required.", "Name")
            .Rule(!string.IsNullOrWhiteSpace(TotalAmountText), "Total amount is required.", "TotalAmount")
            .Rule(decimal.TryParse(TotalAmountText, out var t) && t > 0, "Enter a valid total amount.", "TotalAmount")))
            return;

        decimal.TryParse(PaidAmountText, out var paid);
        var total = decimal.Parse(TotalAmountText);
        var dto = new DebtorDto(DebtorName, Phone, total, paid, DebtDate, Note);

        if (_editingId.HasValue)
        {
            await debtorService.UpdateAsync(_editingId.Value, dto, ct);
            SuccessMessage = "Debtor updated.";
        }
        else
        {
            await debtorService.CreateAsync(dto, ct);
            SuccessMessage = "Debtor added.";
        }

        ResetForm();
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private void Edit(Debtor? item)
    {
        if (item is null) return;

        _editingId = item.Id;
        DebtorName = item.Name;
        Phone = item.Phone;
        TotalAmountText = item.TotalAmount.ToString("F0");
        PaidAmountText = item.PaidAmount.ToString("F0");
        DebtDate = item.Date;
        Note = item.Note;
        IsEditing = true;
        SaveButtonText = "Update";
    }

    [RelayCommand]
    private Task DeleteAsync(Debtor? item) => RunAsync(async ct =>
    {
        if (item is null) return;
        await debtorService.DeleteAsync(item.Id, ct);
        SuccessMessage = "Debtor deleted.";
        ResetForm();
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
    private void SendWhatsAppReminder(Debtor? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.Phone)) return;
        var phone = item.Phone.TrimStart('+').Replace(" ", "");
        if (!phone.StartsWith("91")) phone = "91" + phone;
        var message = Uri.EscapeDataString(
            $"Reminder: You have a pending balance of \u20b9{item.Balance:N0}. " +
            $"Please clear it at your earliest convenience. Thank you!");
        Process.Start(new ProcessStartInfo($"https://wa.me/{phone}?text={message}") { UseShellExecute = true });
    }

    [RelayCommand]
    private void ExportCsv()
    {
        if (Debtors.Count == 0) return;
        if (CsvExporter.Export(Debtors, "Debtors.csv"))
            SuccessMessage = "Exported to CSV.";
    }

    private void ResetForm()
    {
        _editingId = null;
        DebtorName = string.Empty;
        Phone = string.Empty;
        TotalAmountText = string.Empty;
        PaidAmountText = string.Empty;
        DebtDate = DateTime.Today;
        Note = string.Empty;
        IsEditing = false;
        SaveButtonText = "Save";
    }

    private void ApplyFilters()
    {
        IEnumerable<Debtor> query = _allItems;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(d =>
                d.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                d.Phone.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (ActiveStatusFilter == "Pending")
            query = query.Where(d => d.Balance > 0);
        else if (ActiveStatusFilter == "Cleared")
            query = query.Where(d => d.Balance <= 0);

        var list = query.ToList();
        Debtors = new ObservableCollection<Debtor>(list);
        HasItems = list.Count > 0;
        FilterCountText = ActiveStatusFilter == "All" && string.IsNullOrWhiteSpace(SearchText) ? "" : $"{list.Count} debtors";
    }

    private async Task ReloadAsync(CancellationToken ct)
    {
        var stats = await debtorService.GetStatsAsync(ct);
        TotalDebtors = stats.TotalDebtors;
        PendingDebtors = stats.PendingDebtors;
        TotalOutstanding = stats.TotalOutstanding;

        var items = await debtorService.GetAllAsync(ct);
        _allItems = [.. items];
        ApplyFilters();
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
