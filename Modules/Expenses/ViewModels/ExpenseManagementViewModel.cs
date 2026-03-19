using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Expenses.Services;

namespace StoreAssistantPro.Modules.Expenses.ViewModels;

public partial class ExpenseManagementViewModel(IExpenseService expenseService) : BaseViewModel
{
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
    private Task SearchAsync() => RunAsync(async ct =>
    {
        CurrentPage = 1;
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task SetDateFilterAsync(string filter) => RunAsync(async ct =>
    {
        ActiveDateFilter = filter;
        CurrentPage = 1;
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task PreviousPageAsync() => RunAsync(async ct =>
    {
        if (!HasPreviousPage) return;
        CurrentPage--;
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task NextPageAsync() => RunAsync(async ct =>
    {
        if (!HasNextPage) return;
        CurrentPage++;
        await ReloadAsync(ct);
    });

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

    private async Task ReloadAsync(CancellationToken ct)
    {
        var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText;
        var dateFilter = ActiveDateFilter;

        var statsTask = expenseService.GetStatsAsync(ct);
        var pageTask = expenseService.GetPagedAsync(new PagedQuery(CurrentPage, PageSize), search, dateFilter, ct);
        var depositsTask = expenseService.GetDepositsAsync(ct);

        await Task.WhenAll(statsTask, pageTask, depositsTask);

        var stats = statsTask.Result;
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

        var result = pageTask.Result;
        Expenses = new ObservableCollection<Expense>(result.Items);
        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages == 0 ? 1 : result.TotalPages;
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
        HasItems = result.TotalCount > 0;
        FilterCountText = (dateFilter != "All" || search is not null)
            ? $"{result.TotalCount} entries"
            : string.Empty;
        PagingInfo = TotalCount > 0
            ? $"Page {CurrentPage} of {TotalPages} ({TotalCount} total)"
            : string.Empty;

        Deposits = new ObservableCollection<PettyCashDeposit>(depositsTask.Result);
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
