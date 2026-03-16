using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Expenses.Services;

namespace StoreAssistantPro.Modules.Expenses.ViewModels;

public partial class ExpenseManagementViewModel(IExpenseService expenseService) : BaseViewModel
{
    private List<Expense> _allItems = [];

    [ObservableProperty]
    public partial ObservableCollection<Expense> Expenses { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<PettyCashDeposit> Deposits { get; set; } = [];

    [ObservableProperty]
    public partial decimal TotalExpenses { get; set; }

    [ObservableProperty]
    public partial decimal TotalDeposits { get; set; }

    [ObservableProperty]
    public partial decimal Balance { get; set; }

    [ObservableProperty]
    public partial int ExpenseCount { get; set; }

    [ObservableProperty]
    public partial decimal TodaySpent { get; set; }

    [ObservableProperty]
    public partial decimal ThisMonthSpent { get; set; }

    [ObservableProperty]
    public partial decimal LastMonthSpent { get; set; }

    [ObservableProperty]
    public partial bool IsLowBalance { get; set; }

    [ObservableProperty]
    public partial string MonthlyComparisonText { get; set; } = string.Empty;

    // ── Form fields ──

    [ObservableProperty]
    public partial DateTime ExpenseDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial string Category { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AmountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial string SaveButtonText { get; set; } = "Save";

    [ObservableProperty]
    public partial Expense? SelectedExpense { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ActiveDateFilter { get; set; } = "All";

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
            .Rule(!string.IsNullOrWhiteSpace(Category), "Category is required.", "Category")
            .Rule(!string.IsNullOrWhiteSpace(AmountText), "Amount is required.", "Amount")
            .Rule(decimal.TryParse(AmountText, out var amt) && amt > 0, "Amount must be a positive number.", "Amount")))
            return;

        var amount = decimal.Parse(AmountText);
        var dto = new ExpenseDto(ExpenseDate, Category, amount);

        if (_editingId.HasValue)
        {
            await expenseService.UpdateAsync(_editingId.Value, dto, ct);
            SuccessMessage = "Expense updated.";
        }
        else
        {
            await expenseService.CreateAsync(dto, ct);
            SuccessMessage = "Expense added.";
        }

        ClearForm();
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private void Edit(Expense? item)
    {
        if (item is null) return;

        _editingId = item.Id;
        ExpenseDate = item.Date;
        Category = item.Category;
        AmountText = item.Amount.ToString("F0");
        IsEditing = true;
        SaveButtonText = "Update";
    }

    [RelayCommand]
    private Task DeleteAsync(Expense? item) => RunAsync(async ct =>
    {
        if (item is null) return;
        await expenseService.DeleteAsync(item.Id, ct);
        SuccessMessage = "Expense deleted.";
        ClearForm();
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
        if (Expenses.Count == 0) return;
        if (CsvExporter.Export(Expenses, "Expenses.csv"))
            SuccessMessage = "Exported to CSV.";
    }

    [RelayCommand]
    private Task ImportCsvAsync() => RunAsync(async ct =>
    {
        ClearMessages();
        var rows = CsvImporter.Import();
        if (rows is null) return;
        if (rows.Count == 0) { ErrorMessage = "CSV file is empty."; return; }

        var imported = await expenseService.ImportBulkAsync(rows, ct);
        SuccessMessage = $"Imported {imported} expense(s).";
        await ReloadAsync(ct);
    });

    private void ResetForm()
    {
        _editingId = null;
        ExpenseDate = DateTime.Today;
        Category = string.Empty;
        AmountText = string.Empty;
        IsEditing = false;
        SaveButtonText = "Save";
    }

    private void ApplyFilters()
    {
        var today = DateTime.Today;
        IEnumerable<Expense> query = _allItems;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(e =>
                e.Category.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (ActiveDateFilter != "All")
        {
            query = ActiveDateFilter switch
            {
                "Today" => query.Where(e => e.Date.Date == today),
                "Week" => query.Where(e => e.Date.Date >= today.AddDays(-(int)today.DayOfWeek)),
                "Month" => query.Where(e => e.Date.Year == today.Year && e.Date.Month == today.Month),
                _ => query
            };
        }

        var list = query.ToList();
        Expenses = new ObservableCollection<Expense>(list);
        HasItems = list.Count > 0;
        FilterCountText = ActiveDateFilter == "All" && string.IsNullOrWhiteSpace(SearchText) ? "" : $"{list.Count} entries";
    }

    private async Task ReloadAsync(CancellationToken ct)
    {
        var stats = await expenseService.GetStatsAsync(ct);
        TotalExpenses = stats.TotalExpenses;
        TotalDeposits = stats.TotalDeposits;
        Balance = stats.Balance;
        ExpenseCount = stats.ExpenseCount;
        TodaySpent = stats.TodaySpent;
        ThisMonthSpent = stats.ThisMonthSpent;
        LastMonthSpent = stats.LastMonthSpent;
        IsLowBalance = stats.Balance < 1000;

        if (stats.LastMonthSpent > 0)
        {
            var diff = stats.ThisMonthSpent - stats.LastMonthSpent;
            var pct = diff / stats.LastMonthSpent * 100;
            MonthlyComparisonText = diff >= 0 ? $"+{pct:N0}% vs last month" : $"{pct:N0}% vs last month";
        }
        else
        {
            MonthlyComparisonText = string.Empty;
        }

        var items = await expenseService.GetAllAsync(ct);
        _allItems = [.. items];
        ApplyFilters();

        var deposits = await expenseService.GetDepositsAsync(ct);
        Deposits = new ObservableCollection<PettyCashDeposit>(deposits);
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
