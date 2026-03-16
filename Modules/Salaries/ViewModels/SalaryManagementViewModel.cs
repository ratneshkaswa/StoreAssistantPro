using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Salaries.Services;

namespace StoreAssistantPro.Modules.Salaries.ViewModels;

public partial class SalaryManagementViewModel(ISalaryService salaryService) : BaseViewModel
{
    private List<Salary> _allItems = [];

    [ObservableProperty]
    public partial ObservableCollection<Salary> Salaries { get; set; } = [];

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    [ObservableProperty]
    public partial int PaidCount { get; set; }

    [ObservableProperty]
    public partial int UnpaidCount { get; set; }

    [ObservableProperty]
    public partial decimal TotalPaid { get; set; }

    [ObservableProperty]
    public partial decimal TotalPending { get; set; }

    // ── Form fields ──

    [ObservableProperty]
    public partial string EmployeeName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedMonthIndex { get; set; } = DateTime.Today.Month - 1;

    [ObservableProperty]
    public partial string YearText { get; set; } = DateTime.Today.Year.ToString();

    [ObservableProperty]
    public partial string BaseSalaryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AdvanceText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PresentDaysText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AbsentDaysText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HoursWorkedText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string IncentiveText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial string SaveButtonText { get; set; } = "Save";

    [ObservableProperty]
    public partial Salary? SelectedSalary { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ActivePaidFilter { get; set; } = "All";

    [ObservableProperty]
    public partial string FilterCountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasItems { get; set; } = true;

    private int? _editingId;

    private static readonly string[] MonthNames =
        ["January", "February", "March", "April", "May", "June",
         "July", "August", "September", "October", "November", "December"];

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
            .Rule(!string.IsNullOrWhiteSpace(EmployeeName), "Employee name is required.", "Employee")
            .Rule(!string.IsNullOrWhiteSpace(BaseSalaryText), "Base salary is required.", "BaseSalary")
            .Rule(decimal.TryParse(BaseSalaryText, out var bs) && bs > 0, "Enter a valid base salary.", "BaseSalary")))
            return;

        var baseSalary = decimal.Parse(BaseSalaryText);
        decimal.TryParse(AdvanceText, out var advance);
        int.TryParse(PresentDaysText, out var present);
        int.TryParse(AbsentDaysText, out var absent);
        decimal.TryParse(HoursWorkedText, out var hours);
        decimal.TryParse(IncentiveText, out var incentive);
        int.TryParse(YearText, out var year);

        var month = SelectedMonthIndex >= 0 && SelectedMonthIndex < 12
            ? MonthNames[SelectedMonthIndex]
            : MonthNames[0];

        var dto = new SalaryDto(EmployeeName, month, year, baseSalary, baseSalary, advance, present, absent, hours, incentive, null);

        if (_editingId.HasValue)
        {
            await salaryService.UpdateAsync(_editingId.Value, dto, ct);
            SuccessMessage = "Salary updated.";
        }
        else
        {
            await salaryService.CreateAsync(dto, ct);
            SuccessMessage = "Salary added.";
        }

        ResetForm();
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private void Edit(Salary? item)
    {
        if (item is null) return;

        _editingId = item.Id;
        EmployeeName = item.EmployeeName;
        SelectedMonthIndex = Array.IndexOf(MonthNames, item.Month);
        YearText = item.Year.ToString();
        BaseSalaryText = item.BaseSalary.ToString("F0");
        AdvanceText = item.Advance > 0 ? item.Advance.ToString("F0") : string.Empty;
        PresentDaysText = item.PresentDays > 0 ? item.PresentDays.ToString() : string.Empty;
        AbsentDaysText = item.AbsentDays > 0 ? item.AbsentDays.ToString() : string.Empty;
        HoursWorkedText = item.HoursWorked > 0 ? item.HoursWorked.ToString("F1") : string.Empty;
        IncentiveText = item.Incentive > 0 ? item.Incentive.ToString("F0") : string.Empty;
        IsEditing = true;
        SaveButtonText = "Update";
    }

    [RelayCommand]
    private Task DeleteAsync(Salary? item) => RunAsync(async ct =>
    {
        if (item is null) return;
        await salaryService.DeleteAsync(item.Id, ct);
        SuccessMessage = "Salary deleted.";
        ResetForm();
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task MarkPaidAsync(Salary? item) => RunAsync(async ct =>
    {
        if (item is null) return;
        await salaryService.MarkPaidAsync(item.Id, ct);
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
        if (Salaries.Count == 0) return;
        if (CsvExporter.Export(Salaries, "Salaries.csv"))
            SuccessMessage = "Exported to CSV.";
    }

    private void ResetForm()
    {
        _editingId = null;
        EmployeeName = string.Empty;
        SelectedMonthIndex = DateTime.Today.Month - 1;
        YearText = DateTime.Today.Year.ToString();
        BaseSalaryText = string.Empty;
        AdvanceText = string.Empty;
        PresentDaysText = string.Empty;
        AbsentDaysText = string.Empty;
        HoursWorkedText = string.Empty;
        IncentiveText = string.Empty;
        IsEditing = false;
        SaveButtonText = "Save";
    }

    private void ApplyFilters()
    {
        IEnumerable<Salary> query = _allItems;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(s =>
                s.EmployeeName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.Month.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (ActivePaidFilter == "Paid")
            query = query.Where(s => s.IsPaid);
        else if (ActivePaidFilter == "Unpaid")
            query = query.Where(s => !s.IsPaid);

        var list = query.ToList();
        Salaries = new ObservableCollection<Salary>(list);
        HasItems = list.Count > 0;
        FilterCountText = ActivePaidFilter == "All" && string.IsNullOrWhiteSpace(SearchText) ? "" : $"{list.Count} records";
    }

    private async Task ReloadAsync(CancellationToken ct)
    {
        var stats = await salaryService.GetStatsAsync(ct);
        TotalCount = stats.Total;
        PaidCount = stats.Paid;
        UnpaidCount = stats.Unpaid;
        TotalPaid = stats.TotalPaid;
        TotalPending = stats.TotalPending;

        var items = await salaryService.GetAllAsync(ct);
        _allItems = [.. items];
        ApplyFilters();
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
