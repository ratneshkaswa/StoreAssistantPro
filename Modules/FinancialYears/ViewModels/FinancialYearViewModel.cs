using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.FinancialYears.Services;

namespace StoreAssistantPro.Modules.FinancialYears.ViewModels;

public partial class FinancialYearViewModel(
    IFinancialYearService fyService,
    IRegionalSettingsService regional) : BaseViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<FinancialYear> FinancialYears { get; set; } = [];

    [ObservableProperty]
    public partial FinancialYear? CurrentYear { get; set; }

    [ObservableProperty]
    public partial FinancialYear? SelectedYear { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingReset { get; set; }

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        var years = await fyService.GetAllAsync(ct);
        FinancialYears = new ObservableCollection<FinancialYear>(years);
        CurrentYear = await fyService.GetCurrentAsync(ct);
    });

    [RelayCommand]
    private Task CreateNewYearAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;
        var now = regional.Now;
        var nextApril = now.Month >= 4
            ? new DateTime(now.Year + 1, 4, 1)
            : new DateTime(now.Year, 4, 1);

        await fyService.CreateAsync(nextApril, ct);
        SuccessMessage = "New financial year created.";
        await LoadAsync();
    });

    [RelayCommand]
    private Task SetCurrentAsync() => RunAsync(async ct =>
    {
        if (SelectedYear is null)
        {
            ErrorMessage = "Select a financial year first.";
            return;
        }

        SuccessMessage = string.Empty;
        IsConfirmingReset = false;
        await fyService.SetCurrentAsync(SelectedYear.Id, ct);
        SuccessMessage = $"Active year set to {SelectedYear.Name}.";
        await LoadAsync();
    });

    [RelayCommand]
    private void ShowResetConfirmation()
    {
        IsConfirmingReset = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    [RelayCommand]
    private void CancelReset()
    {
        IsConfirmingReset = false;
    }

    [RelayCommand]
    private Task ConfirmResetAsync() => RunAsync(async ct =>
    {
        SuccessMessage = string.Empty;
        await fyService.EnsureCurrentYearAsync(ct);
        IsConfirmingReset = false;
        SuccessMessage = "Billing numbers have been reset for the current financial year.";
        await LoadAsync();
    });
}
