using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.MainShell.Models;
using StoreAssistantPro.Modules.MainShell.Services;

namespace StoreAssistantPro.Modules.MainShell.ViewModels;

public sealed partial class WorkspaceViewModel(
    IDashboardService dashboardService,
    IRegionalSettingsService regional) : BaseViewModel
{
    // ── Sales KPIs ──

    [ObservableProperty]
    public partial decimal TodaySales { get; set; }

    [ObservableProperty]
    public partial int TodayTransactions { get; set; }

    [ObservableProperty]
    public partial decimal ThisMonthSales { get; set; }

    [ObservableProperty]
    public partial int ThisMonthTransactions { get; set; }

    // ── Inventory KPIs ──

    [ObservableProperty]
    public partial int TotalProducts { get; set; }

    [ObservableProperty]
    public partial int LowStockCount { get; set; }

    [ObservableProperty]
    public partial int OutOfStockCount { get; set; }

    // ── Orders & Receivables ──

    [ObservableProperty]
    public partial int PendingOrdersCount { get; set; }

    [ObservableProperty]
    public partial int OverdueOrdersCount { get; set; }

    [ObservableProperty]
    public partial decimal OutstandingReceivables { get; set; }

    // ── Collections ──

    [ObservableProperty]
    public partial ObservableCollection<RecentSaleItem> RecentSales { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<TopProductItem> TopProducts { get; set; }

    // ── Derived display values ──

    public string TodaySalesFormatted => regional.FormatCurrency(TodaySales);
    public string ThisMonthSalesFormatted => regional.FormatCurrency(ThisMonthSales);
    public string ReceivablesFormatted => regional.FormatCurrency(OutstandingReceivables);
    public bool HasStockAlerts => LowStockCount > 0 || OutOfStockCount > 0;
    public bool HasOverdueOrders => OverdueOrdersCount > 0;

    // ── Commands ──

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        var summary = await dashboardService.GetSummaryAsync(ct);
        ApplySummary(summary);
    });

    [RelayCommand]
    private Task RefreshAsync() => RunLoadAsync(async ct =>
    {
        var summary = await dashboardService.GetSummaryAsync(ct);
        ApplySummary(summary);
    });

    private void ApplySummary(DashboardSummary summary)
    {
        TodaySales = summary.TodaySales;
        TodayTransactions = summary.TodayTransactions;
        ThisMonthSales = summary.ThisMonthSales;
        ThisMonthTransactions = summary.ThisMonthTransactions;
        TotalProducts = summary.TotalProducts;
        LowStockCount = summary.LowStockCount;
        OutOfStockCount = summary.OutOfStockCount;
        PendingOrdersCount = summary.PendingOrdersCount;
        OverdueOrdersCount = summary.OverdueOrdersCount;
        OutstandingReceivables = summary.OutstandingReceivables;

        RecentSales = new(summary.RecentSales);
        TopProducts = new(summary.TopProducts);

        // Notify derived properties
        OnPropertyChanged(nameof(TodaySalesFormatted));
        OnPropertyChanged(nameof(ThisMonthSalesFormatted));
        OnPropertyChanged(nameof(ReceivablesFormatted));
        OnPropertyChanged(nameof(HasStockAlerts));
        OnPropertyChanged(nameof(HasOverdueOrders));
    }
}
