using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Payments.Services;

namespace StoreAssistantPro.Modules.Payments.ViewModels;

public partial class PaymentManagementViewModel(IPaymentService paymentService) : BaseViewModel
{
    private List<Payment> _allItems = [];

    [ObservableProperty]
    public partial ObservableCollection<Payment> Payments { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Customer> Customers { get; set; } = [];

    [ObservableProperty]
    public partial int TotalPayments { get; set; }

    [ObservableProperty]
    public partial decimal TotalAmount { get; set; }

    // ── Form fields ──

    [ObservableProperty]
    public partial Customer? SelectedCustomer { get; set; }

    [ObservableProperty]
    public partial DateTime PaymentDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial string AmountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial string SaveButtonText { get; set; } = "Save";

    [ObservableProperty]
    public partial Payment? SelectedPayment { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ActiveDateFilter { get; set; } = "All";

    [ObservableProperty]
    public partial string FilterCountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal ThisMonthAmount { get; set; }

    [ObservableProperty]
    public partial bool HasItems { get; set; } = true;

    private int? _editingId;

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        var customers = await paymentService.GetCustomersAsync(ct);
        Customers = new ObservableCollection<Customer>(customers);
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v
            .Rule(SelectedCustomer is not null, "Customer is required.", "Customer")
            .Rule(!string.IsNullOrWhiteSpace(AmountText), "Amount is required.", "Amount")
            .Rule(decimal.TryParse(AmountText, out var amt) && amt > 0, "Enter a valid amount.", "Amount")))
            return;

        var amount = decimal.Parse(AmountText);
        var dto = new PaymentDto(SelectedCustomer!.Id, PaymentDate, amount, Note);

        if (_editingId.HasValue)
        {
            await paymentService.UpdateAsync(_editingId.Value, dto, ct);
            SuccessMessage = "Payment updated.";
        }
        else
        {
            await paymentService.CreateAsync(dto, ct);
            SuccessMessage = "Payment recorded.";
        }

        ResetForm();
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private void Edit(Payment? item)
    {
        if (item is null) return;

        _editingId = item.Id;
        SelectedCustomer = Customers.FirstOrDefault(c => c.Id == item.CustomerId);
        PaymentDate = item.PaymentDate;
        AmountText = item.Amount.ToString("F0");
        Note = item.Note;
        IsEditing = true;
        SaveButtonText = "Update";
    }

    [RelayCommand]
    private Task DeleteAsync(Payment? item) => RunAsync(async ct =>
    {
        if (item is null) return;
        await paymentService.DeleteAsync(item.Id, ct);
        SuccessMessage = "Payment deleted.";
        ResetForm();
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private void ClearForm() => ResetForm();

    [RelayCommand]
    private void Search() => ApplyFilters();

    [RelayCommand]
    private void SetDateFilter(string filter)
    {
        ActiveDateFilter = filter;
        ApplyFilters();
    }

    [RelayCommand]
    private void ExportCsv()
    {
        if (Payments.Count == 0) return;
        if (CsvExporter.Export(Payments, "Payments.csv"))
            SuccessMessage = "Exported to CSV.";
    }

    private void ResetForm()
    {
        _editingId = null;
        SelectedCustomer = null;
        PaymentDate = DateTime.Today;
        AmountText = string.Empty;
        Note = string.Empty;
        IsEditing = false;
        SaveButtonText = "Save";
    }

    private void ApplyFilters()
    {
        var today = DateTime.Today;
        IEnumerable<Payment> query = _allItems;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(p =>
                (p.Customer?.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                p.Note.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (ActiveDateFilter != "All")
        {
            query = ActiveDateFilter switch
            {
                "Today" => query.Where(p => p.PaymentDate.Date == today),
                "Week" => query.Where(p => p.PaymentDate.Date >= today.AddDays(-(int)today.DayOfWeek)),
                "Month" => query.Where(p => p.PaymentDate.Year == today.Year && p.PaymentDate.Month == today.Month),
                _ => query
            };
        }

        var list = query.ToList();
        Payments = new ObservableCollection<Payment>(list);
        HasItems = list.Count > 0;
        FilterCountText = ActiveDateFilter == "All" && string.IsNullOrWhiteSpace(SearchText) ? "" : $"{list.Count} payments";
    }

    private async Task ReloadAsync(CancellationToken ct)
    {
        var stats = await paymentService.GetStatsAsync(ct);
        TotalPayments = stats.TotalPayments;
        TotalAmount = stats.TotalAmount;

        var items = await paymentService.GetAllAsync(ct);
        _allItems = [.. items];

        var today = DateTime.Today;
        ThisMonthAmount = _allItems
            .Where(p => p.PaymentDate.Year == today.Year && p.PaymentDate.Month == today.Month)
            .Sum(p => p.Amount);

        ApplyFilters();
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
