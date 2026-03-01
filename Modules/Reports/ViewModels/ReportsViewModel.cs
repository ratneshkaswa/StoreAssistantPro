using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Reports.Models;
using StoreAssistantPro.Modules.Reports.Services;

namespace StoreAssistantPro.Modules.Reports.ViewModels;

public partial class ReportsViewModel(
    IReportingService reportingService,
    IRegionalSettingsService regional) : BaseViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<DaySalesSummary> DaySales { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<MonthSalesSummary> MonthSales { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<CategorySalesSummary> CategorySales { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ProfitLossSummary> ProfitLoss { get; set; } = [];

    [ObservableProperty]
    public partial DateTime DateFrom { get; set; } = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    public partial DateTime DateTo { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial decimal TodaysSales { get; set; }

    [ObservableProperty]
    public partial int TodaysBillCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDaySales))]
    [NotifyPropertyChangedFor(nameof(IsMonthSales))]
    [NotifyPropertyChangedFor(nameof(IsCategorySales))]
    [NotifyPropertyChangedFor(nameof(IsProfitLoss))]
    public partial string SelectedReportType { get; set; } = "Day-wise Sales";

    public bool IsDaySales => SelectedReportType == "Day-wise Sales";
    public bool IsMonthSales => SelectedReportType == "Month-wise Sales";
    public bool IsCategorySales => SelectedReportType == "Category Sales";
    public bool IsProfitLoss => SelectedReportType == "Profit & Loss";

    public string[] ReportTypes { get; } =
        ["Day-wise Sales", "Month-wise Sales", "Category Sales", "Profit & Loss"];

    [RelayCommand]
    private Task LoadReportsAsync() => RunLoadAsync(async _ =>
    {
        TodaysSales = await reportingService.GetTodaysSalesAsync();
        TodaysBillCount = await reportingService.GetTodaysBillCountAsync();
        await RefreshCurrentReportAsync();
    });

    [RelayCommand]
    private async Task RefreshCurrentReportAsync()
    {
        var from = DateFrom;
        var to = DateTo.AddDays(1);

        switch (SelectedReportType)
        {
            case "Day-wise Sales":
                DaySales = new ObservableCollection<DaySalesSummary>(
                    await reportingService.GetDayWiseSalesAsync(from, to));
                break;
            case "Month-wise Sales":
                MonthSales = new ObservableCollection<MonthSalesSummary>(
                    await reportingService.GetMonthWiseSalesAsync(from, to));
                break;
            case "Category Sales":
                CategorySales = new ObservableCollection<CategorySalesSummary>(
                    await reportingService.GetCategoryWiseSalesAsync(from, to));
                break;
            case "Profit & Loss":
                ProfitLoss = new ObservableCollection<ProfitLossSummary>(
                    await reportingService.GetProfitLossAsync(from, to));
                break;
        }
    }

    partial void OnSelectedReportTypeChanged(string value)
    {
        if (!IsLoading)
            _ = RefreshCurrentReportAsync();
    }
}
