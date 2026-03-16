using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Reports.Services;

namespace StoreAssistantPro.Modules.Reports.ViewModels;

public partial class ReportsViewModel(IReportsService reportsService) : BaseViewModel
{
    // ── Date range ──

    [ObservableProperty]
    public partial DateTime DateFrom { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    public partial DateTime DateTo { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial string ActivePreset { get; set; } = "This Month";

    // ── Expense report ──

    [ObservableProperty]
    public partial int ExpenseCount { get; set; }

    [ObservableProperty]
    public partial decimal ExpenseTotal { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<CategoryBreakdown> ExpenseByCategory { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<MonthlyTotal> ExpenseMonthlyTrend { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Expense> ExpenseRecent { get; set; } = [];

    // ── Ironing report ──

    [ObservableProperty]
    public partial int IroningCount { get; set; }

    [ObservableProperty]
    public partial decimal IroningTotal { get; set; }

    [ObservableProperty]
    public partial decimal IroningPaid { get; set; }

    [ObservableProperty]
    public partial decimal IroningUnpaid { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<IroningEntry> IroningRecent { get; set; } = [];

    // ── Order report ──

    [ObservableProperty]
    public partial int OrderCount { get; set; }

    [ObservableProperty]
    public partial decimal OrderTotal { get; set; }

    [ObservableProperty]
    public partial int OrderDelivered { get; set; }

    [ObservableProperty]
    public partial int OrderPending { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Order> OrderRecent { get; set; } = [];

    // ── Inward report ──

    [ObservableProperty]
    public partial int InwardCount { get; set; }

    [ObservableProperty]
    public partial decimal InwardTotal { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<InwardEntry> InwardRecent { get; set; } = [];

    // ── Debtor report ──

    [ObservableProperty]
    public partial int DebtorCount { get; set; }

    [ObservableProperty]
    public partial decimal DebtorOutstanding { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<TopDebtor> TopDebtors { get; set; } = [];

    // ── Commands ──

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        await RefreshAllAsync(ct);
    });

    [RelayCommand]
    private void SetPreset(string preset)
    {
        ActivePreset = preset;
        var today = DateTime.Today;

        (DateFrom, DateTo) = preset switch
        {
            "This Month" => (new DateTime(today.Year, today.Month, 1), today),
            "Last Month" => (new DateTime(today.Year, today.Month, 1).AddMonths(-1),
                             new DateTime(today.Year, today.Month, 1).AddDays(-1)),
            "This Quarter" => (new DateTime(today.Year, (today.Month - 1) / 3 * 3 + 1, 1), today),
            "This Year" => (new DateTime(today.Year, 4, 1) <= today
                            ? new DateTime(today.Year, 4, 1)
                            : new DateTime(today.Year - 1, 4, 1), today),
            "All Time" => (new DateTime(2020, 1, 1), today),
            _ => (DateFrom, DateTo)
        };
    }

    [RelayCommand]
    private Task RefreshAsync() => RunAsync(async ct =>
    {
        await RefreshAllAsync(ct);
        SuccessMessage = "Reports refreshed.";
    });

    [RelayCommand]
    private void ExportExpenseCsv()
    {
        if (ExpenseRecent.Count == 0) return;
        if (CsvExporter.Export(ExpenseRecent, "ExpenseReport.csv"))
            SuccessMessage = "Expense report exported.";
    }

    [RelayCommand]
    private void ExportIroningCsv()
    {
        if (IroningRecent.Count == 0) return;
        if (CsvExporter.Export(IroningRecent, "IroningReport.csv"))
            SuccessMessage = "Ironing report exported.";
    }

    [RelayCommand]
    private void ExportOrderCsv()
    {
        if (OrderRecent.Count == 0) return;
        if (CsvExporter.Export(OrderRecent, "OrderReport.csv"))
            SuccessMessage = "Order report exported.";
    }

    [RelayCommand]
    private void ExportInwardCsv()
    {
        if (InwardRecent.Count == 0) return;
        if (CsvExporter.Export(InwardRecent, "InwardReport.csv"))
            SuccessMessage = "Inward report exported.";
    }

    private async Task RefreshAllAsync(CancellationToken ct)
    {
        var from = DateFrom;
        var to = DateTo.Date.AddDays(1).AddTicks(-1);

        var expense = await reportsService.GetExpenseReportAsync(from, to, ct);
        ExpenseCount = expense.Count;
        ExpenseTotal = expense.Total;
        ExpenseByCategory = new ObservableCollection<CategoryBreakdown>(expense.ByCategory);
        ExpenseMonthlyTrend = new ObservableCollection<MonthlyTotal>(expense.MonthlyTrend);
        ExpenseRecent = new ObservableCollection<Expense>(expense.RecentEntries);

        var ironing = await reportsService.GetIroningReportAsync(from, to, ct);
        IroningCount = ironing.Count;
        IroningTotal = ironing.Total;
        IroningPaid = ironing.PaidTotal;
        IroningUnpaid = ironing.UnpaidTotal;
        IroningRecent = new ObservableCollection<IroningEntry>(ironing.RecentEntries);

        var order = await reportsService.GetOrderReportAsync(from, to, ct);
        OrderCount = order.Count;
        OrderTotal = order.Total;
        OrderDelivered = order.Delivered;
        OrderPending = order.Pending;
        OrderRecent = new ObservableCollection<Order>(order.RecentEntries);

        var inward = await reportsService.GetInwardReportAsync(from, to, ct);
        InwardCount = inward.Count;
        InwardTotal = inward.Total;
        InwardRecent = new ObservableCollection<InwardEntry>(inward.RecentEntries);

        var debtor = await reportsService.GetDebtorReportAsync(ct);
        DebtorCount = debtor.Count;
        DebtorOutstanding = debtor.TotalOutstanding;
        TopDebtors = new ObservableCollection<TopDebtor>(debtor.TopDebtors);
    }
}
