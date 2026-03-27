using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Controls;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Billing.Events;
using StoreAssistantPro.Modules.MainShell.Models;
using StoreAssistantPro.Modules.MainShell.Services;

namespace StoreAssistantPro.Modules.MainShell.ViewModels;

public sealed partial class WorkspaceViewModel(
    IDashboardService dashboardService,
    IRegionalSettingsService regional,
    IEventBus eventBus) : BaseViewModel, INavigationAware
{
    private static readonly TimeSpan AutoRefreshInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan NavigationFreshnessWindow = TimeSpan.FromMinutes(2);
    private DispatcherTimer? _autoRefreshTimer;
    private DateTime _lastRefreshedAt = regional.Now;
    private bool _salesChangeSubscribed;

    public IReadOnlyList<string> BreadcrumbItems { get; } = ["Home", "Dashboard"];
    public string CurrencySymbol => regional.CurrencySymbol;
    public string HeaderSubtitle => IsViewingPastDate
        ? $"Viewing dashboard snapshot for {regional.FormatDate(SelectedDate)}."
        : "Track live counter, stock, and collections here. Use Reports for deeper analysis.";
    public string LastUpdatedText => $"Updated {FormatRelativeTime(_lastRefreshedAt)}";

    // #410 Dashboard date selector
    [ObservableProperty]
    public partial DateTime SelectedDate { get; set; } = regional.Now.Date;

    public bool IsViewingPastDate => SelectedDate.Date < regional.Now.Date;
    public string DateLabel => IsViewingPastDate
        ? regional.FormatDate(SelectedDate)
        : "Today";

    partial void OnSelectedDateChanged(DateTime value)
    {
        OnPropertyChanged(nameof(IsViewingPastDate));
        OnPropertyChanged(nameof(DateLabel));
        OnPropertyChanged(nameof(HeaderSubtitle));
        RestartAutoRefreshForCurrentContext();
        _ = RefreshForDateAsync();
    }

    [RelayCommand]
    private void ResetToToday()
    {
        SelectedDate = regional.Now.Date;
    }

    private Task RefreshForDateAsync() => RunTrackedLoadAsync(async ct =>
    {
        var summary = await LoadSelectedDateSummaryAsync(ct);
        ApplySummary(summary);
    });

    // Sales KPIs

    [ObservableProperty]
    public partial decimal TodaySales { get; set; }

    [ObservableProperty]
    public partial int TodayTransactions { get; set; }

    [ObservableProperty]
    public partial decimal TodayReturns { get; set; }

    [ObservableProperty]
    public partial decimal TodayNetSales { get; set; }

    [ObservableProperty]
    public partial decimal TodayProfit { get; set; }

    [ObservableProperty]
    public partial decimal AverageBillValue { get; set; }

    [ObservableProperty]
    public partial decimal ThisMonthSales { get; set; }

    [ObservableProperty]
    public partial int ThisMonthTransactions { get; set; }

    // Inventory KPIs

    [ObservableProperty]
    public partial int TotalProducts { get; set; }

    [ObservableProperty]
    public partial int LowStockCount { get; set; }

    [ObservableProperty]
    public partial int OutOfStockCount { get; set; }

    // Orders and receivables

    [ObservableProperty]
    public partial int PendingOrdersCount { get; set; }

    [ObservableProperty]
    public partial int OverdueOrdersCount { get; set; }

    [ObservableProperty]
    public partial decimal OutstandingReceivables { get; set; }

    [ObservableProperty]
    public partial int PendingPaymentsCount { get; set; }

    private DateTime? _lastBackupDate;
    private decimal _previousDaySales;
    private decimal _previousDayReturns;
    private decimal _previousDayNetSales;
    private decimal _previousDayAverageBillValue;
    private decimal _previousMonthSales;

    // Display collections

    [ObservableProperty]
    public partial ObservableCollection<RecentSaleDisplayItem> RecentSalesDisplay { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<TopProductDisplayItem> TopProductsTodayDisplay { get; set; } = [];

    // #398 Monthly sales trend
    [ObservableProperty]
    public partial ObservableCollection<DailySalesTrendDisplayItem> DailySalesTrendDisplay { get; set; } = [];

    // Derived display values

    public string TodaySalesFormatted => regional.FormatCurrency(TodaySales);
    public string TodaySalesCompact => FormatCompactCurrency(TodaySales);
    public string TodayTransactionsLabel => BuildCountLabel(TodayTransactions, "transaction");
    public KpiTrendDisplayItem TodaySalesTrend => BuildTrend(TodaySales, _previousDaySales, "yesterday");

    public string TodayReturnsCompact => FormatCompactCurrency(TodayReturns);
    public string TodayReturnsLabel => TodayReturns == 0 ? "No returns today" : "Returned today";
    public KpiTrendDisplayItem TodayReturnsTrend => BuildTrend(TodayReturns, _previousDayReturns, "yesterday", higherIsBetter: false);

    public string TodayNetSalesCompact => FormatCompactCurrency(TodayNetSales);
    public string TodayNetSalesLabel => "Sales − Returns";
    public KpiTrendDisplayItem TodayNetSalesTrend => BuildTrend(TodayNetSales, _previousDayNetSales, "yesterday");

    public string TodayProfitCompact => FormatCompactCurrency(TodayProfit);
    public string TodayProfitLabel => TodayProfit >= 0 ? "Estimated profit" : "Estimated loss";

    public string AverageBillCompact => FormatCompactCurrency(AverageBillValue);
    public string AverageBillLabel => TodayTransactions == 0 ? "No bills yet" : "Avg. bill value";
    public KpiTrendDisplayItem AverageBillTrend => BuildTrend(AverageBillValue, _previousDayAverageBillValue, "yesterday");

    public string ThisMonthSalesFormatted => regional.FormatCurrency(ThisMonthSales);
    public string ThisMonthSalesCompact => FormatCompactCurrency(ThisMonthSales);
    public string ThisMonthTransactionsLabel => BuildCountLabel(ThisMonthTransactions, "bill");
    public KpiTrendDisplayItem ThisMonthSalesTrend => BuildTrend(ThisMonthSales, _previousMonthSales, "last month");

    public string TotalProductsCompact => FormatCompactNumber(TotalProducts);
    public string ProductStatusLabel => OutOfStockCount == 0
        ? "All items available"
        : $"{BuildCountLabel(OutOfStockCount, "item")} out of stock";

    public string LowStockCompact => FormatCompactNumber(LowStockCount);
    public string LowStockLabel => LowStockCount == 0
        ? "Stock levels look healthy"
        : $"{BuildCountLabel(LowStockCount, "item")} need reorder";

    public string PendingOrdersCompact => FormatCompactNumber(PendingOrdersCount);
    public string PendingOrdersLabel => OverdueOrdersCount > 0
        ? $"{BuildCountLabel(OverdueOrdersCount, "order")} overdue"
        : PendingOrdersCount == 0
            ? "No pending orders"
            : $"{BuildCountLabel(PendingOrdersCount, "order")} awaiting follow-up";

    public string ReceivablesFormatted => regional.FormatCurrency(OutstandingReceivables);
    public string ReceivablesCompact => FormatCompactCurrency(OutstandingReceivables);
    public string ReceivablesLabel => OutstandingReceivables == 0
        ? "Nothing outstanding"
        : "Awaiting collection";

    public string PendingPaymentsCompact => FormatCompactNumber(PendingPaymentsCount);
    public string PendingPaymentsLabel => PendingPaymentsCount == 0
        ? "All accounts settled"
        : BuildCountLabel(PendingPaymentsCount, "customer") + " with dues";

    public bool HasStockAlerts => LowStockCount > 0 || OutOfStockCount > 0;
    public bool HasOverdueOrders => OverdueOrdersCount > 0;
    public bool HasAlertBanner => BuildAlertSegments().Count > 0;
    public bool IsBackupOverdue => _lastBackupDate is null || (regional.Now - _lastBackupDate.Value).TotalHours >= 24;
    public string AlertTitle => OverdueOrdersCount > 0
        ? "Orders need attention"
        : HasStockAlerts
            ? "Inventory needs attention"
            : PendingOrdersCount > 0
                ? "Pending orders waiting"
                : IsBackupOverdue
                    ? "Backup overdue"
                    : string.Empty;
    public string AlertMessage => string.Join("; ", BuildAlertSegments());
    public InfoBarSeverity AlertSeverity => OverdueOrdersCount > 0
        ? InfoBarSeverity.Error
        : HasStockAlerts
            ? InfoBarSeverity.Warning
            : IsBackupOverdue
                ? InfoBarSeverity.Warning
                : InfoBarSeverity.Info;

    // Commands

    [RelayCommand]
    private Task LoadAsync() => LoadOnActivateAsync(
        async ct =>
        {
            EnsureSalesChangeSubscription();
            var summary = await LoadSelectedDateSummaryAsync(ct);
            ApplySummary(summary);
            RestartAutoRefreshForCurrentContext();
        },
        NavigationFreshnessWindow);

    [RelayCommand]
    private Task RefreshAsync() => RunTrackedLoadAsync(async ct =>
    {
        dashboardService.InvalidateCache();
        var summary = await LoadSelectedDateSummaryAsync(ct);
        ApplySummary(summary);
        RestartAutoRefreshForCurrentContext();
    });

    private void StartAutoRefresh()
    {
        if (_autoRefreshTimer is not null) return;

        _autoRefreshTimer = new DispatcherTimer { Interval = AutoRefreshInterval };
        _autoRefreshTimer.Tick += OnAutoRefreshTick;
        _autoRefreshTimer.Start();
    }

    private async Task OnAutoRefreshTickAsync()
    {
        if (IsLoading || IsBusy || IsViewingPastDate) return;
        await RefreshAsync();
    }

    public Task OnNavigatedTo(CancellationToken ct = default)
    {
        RestartAutoRefreshForCurrentContext();
        return LoadAsync();
    }

    public void OnNavigatedFrom()
    {
        StopAutoRefresh();
    }

    private void OnAutoRefreshTick(object? sender, EventArgs e)
    {
        _ = OnAutoRefreshTickAsync();
    }

    private void StopAutoRefresh()
    {
        if (_autoRefreshTimer is null)
            return;

        _autoRefreshTimer.Stop();
    }

    private void RestartAutoRefreshForCurrentContext()
    {
        if (IsViewingPastDate)
        {
            StopAutoRefresh();
            return;
        }

        StartAutoRefresh();
        _autoRefreshTimer?.Start();
    }

    public override void Dispose()
    {
        if (_salesChangeSubscribed)
        {
            eventBus.Unsubscribe<SalesDataChangedEvent>(HandleSalesDataChangedAsync);
            _salesChangeSubscribed = false;
        }

        if (_autoRefreshTimer is not null)
        {
            _autoRefreshTimer.Stop();
            _autoRefreshTimer.Tick -= OnAutoRefreshTick;
            _autoRefreshTimer = null;
        }
        base.Dispose();
    }

    private void EnsureSalesChangeSubscription()
    {
        if (_salesChangeSubscribed)
            return;

        eventBus.Subscribe<SalesDataChangedEvent>(HandleSalesDataChangedAsync);
        _salesChangeSubscribed = true;
    }

    private Task HandleSalesDataChangedAsync(SalesDataChangedEvent @event)
    {
        dashboardService.InvalidateCache();
        MarkLoadStale();

        if (!IsViewingPastDate && !IsBusy && !IsLoading)
            _ = RefreshAsync();

        return Task.CompletedTask;
    }

    private Task<DashboardSummary> LoadSelectedDateSummaryAsync(CancellationToken ct) =>
        IsViewingPastDate
            ? dashboardService.GetSummaryForDateAsync(SelectedDate, ct)
            : dashboardService.GetSummaryAsync(ct);

    private void ApplySummary(DashboardSummary summary)
    {
        TodaySales = summary.TodaySales;
        TodayTransactions = summary.TodayTransactions;
        TodayReturns = summary.TodayReturns;
        TodayNetSales = summary.TodayNetSales;
        TodayProfit = summary.TodayProfit;
        AverageBillValue = summary.AverageBillValue;
        ThisMonthSales = summary.ThisMonthSales;
        ThisMonthTransactions = summary.ThisMonthTransactions;
        TotalProducts = summary.TotalProducts;
        LowStockCount = summary.LowStockCount;
        OutOfStockCount = summary.OutOfStockCount;
        PendingOrdersCount = summary.PendingOrdersCount;
        OverdueOrdersCount = summary.OverdueOrdersCount;
        OutstandingReceivables = summary.OutstandingReceivables;
        PendingPaymentsCount = summary.PendingPaymentsCount;
        _lastBackupDate = summary.LastBackupDate;
        _previousDaySales = summary.PreviousDaySales;
        _previousDayReturns = summary.PreviousDayReturns;
        _previousDayNetSales = summary.PreviousDayNetSales;
        _previousDayAverageBillValue = summary.PreviousDayAverageBillValue;
        _previousMonthSales = summary.PreviousMonthSales;

        RecentSalesDisplay = new(summary.RecentSales.Select(BuildRecentSaleDisplay));
        TopProductsTodayDisplay = new(summary.TopProductsToday.Select(BuildTopProductDisplay));

        // #398 Monthly sales trend
        var maxDaySales = summary.DailySalesTrend.Count > 0
            ? summary.DailySalesTrend.Max(d => d.TotalSales)
            : 1m;
        DailySalesTrendDisplay = new(summary.DailySalesTrend.Select(d =>
            new DailySalesTrendDisplayItem(
                d.Date.ToString("dd MMM"),
                regional.FormatCurrency(d.TotalSales),
                d.TransactionCount,
                maxDaySales > 0 ? (double)(d.TotalSales / maxDaySales) : 0)));

        _lastRefreshedAt = regional.Now;

        RaiseDisplayPropertiesChanged();
    }

    private RecentSaleDisplayItem BuildRecentSaleDisplay(RecentSaleItem sale) =>
        new(
            sale.InvoiceNumber,
            FormatRelativeTime(sale.SaleDate),
            regional.FormatDateTime(sale.SaleDate),
            regional.FormatCurrency(sale.TotalAmount),
            sale.PaymentMethod,
            BuildCountLabel(sale.ItemCount, "item"));

    private TopProductDisplayItem BuildTopProductDisplay(TopProductItem product) =>
        new(
            product.ProductName,
            BuildCountLabel(product.QuantitySold, "unit"),
            FormatCompactCurrency(product.Revenue),
            regional.FormatCurrency(product.Revenue));

    private void RaiseDisplayPropertiesChanged()
    {
        OnPropertyChanged(nameof(LastUpdatedText));

        OnPropertyChanged(nameof(TodaySalesFormatted));
        OnPropertyChanged(nameof(TodaySalesCompact));
        OnPropertyChanged(nameof(TodayTransactionsLabel));
        OnPropertyChanged(nameof(TodaySalesTrend));

        OnPropertyChanged(nameof(TodayReturnsCompact));
        OnPropertyChanged(nameof(TodayReturnsLabel));
        OnPropertyChanged(nameof(TodayReturnsTrend));
        OnPropertyChanged(nameof(TodayNetSalesCompact));
        OnPropertyChanged(nameof(TodayNetSalesLabel));
        OnPropertyChanged(nameof(TodayNetSalesTrend));
        OnPropertyChanged(nameof(TodayProfitCompact));
        OnPropertyChanged(nameof(TodayProfitLabel));
        OnPropertyChanged(nameof(AverageBillCompact));
        OnPropertyChanged(nameof(AverageBillLabel));
        OnPropertyChanged(nameof(AverageBillTrend));

        OnPropertyChanged(nameof(ThisMonthSalesFormatted));
        OnPropertyChanged(nameof(ThisMonthSalesCompact));
        OnPropertyChanged(nameof(ThisMonthTransactionsLabel));
        OnPropertyChanged(nameof(ThisMonthSalesTrend));

        OnPropertyChanged(nameof(TotalProductsCompact));
        OnPropertyChanged(nameof(ProductStatusLabel));

        OnPropertyChanged(nameof(LowStockCompact));
        OnPropertyChanged(nameof(LowStockLabel));

        OnPropertyChanged(nameof(PendingOrdersCompact));
        OnPropertyChanged(nameof(PendingOrdersLabel));

        OnPropertyChanged(nameof(ReceivablesFormatted));
        OnPropertyChanged(nameof(ReceivablesCompact));
        OnPropertyChanged(nameof(ReceivablesLabel));

        OnPropertyChanged(nameof(PendingPaymentsCompact));
        OnPropertyChanged(nameof(PendingPaymentsLabel));

        OnPropertyChanged(nameof(HasStockAlerts));
        OnPropertyChanged(nameof(HasOverdueOrders));
        OnPropertyChanged(nameof(HasAlertBanner));
        OnPropertyChanged(nameof(AlertTitle));
        OnPropertyChanged(nameof(AlertMessage));
        OnPropertyChanged(nameof(AlertSeverity));
    }

    private List<string> BuildAlertSegments()
    {
        List<string> segments = [];

        if (OutOfStockCount > 0)
            segments.Add($"{BuildCountLabel(OutOfStockCount, "item")} out of stock");

        if (LowStockCount > 0)
            segments.Add($"{BuildCountLabel(LowStockCount, "item")} running low");

        if (OverdueOrdersCount > 0)
            segments.Add($"{BuildCountLabel(OverdueOrdersCount, "order")} overdue");
        else if (PendingOrdersCount > 0)
            segments.Add($"{BuildCountLabel(PendingOrdersCount, "order")} pending");

        if (_lastBackupDate is null || (regional.Now - _lastBackupDate.Value).TotalHours >= 24)
            segments.Add("No backup in the last 24 hours");

        return segments;
    }

    private string FormatRelativeTime(DateTime timestamp)
    {
        var now = regional.Now;
        var delta = now - timestamp;

        if (delta < TimeSpan.Zero)
            return regional.FormatDateTime(timestamp);

        if (delta < TimeSpan.FromMinutes(1))
            return "Just now";

        if (delta < TimeSpan.FromHours(1))
            return $"{Math.Max(1, (int)delta.TotalMinutes)} min ago";

        if (timestamp.Date == now.Date)
            return $"{Math.Max(1, (int)delta.TotalHours)} hr ago";

        if (timestamp.Date == now.Date.AddDays(-1))
            return "Yesterday";

        return regional.FormatDateTime(timestamp);
    }

    private string BuildCountLabel(int value, string singular)
    {
        var plural = value == 1 ? singular : $"{singular}s";
        return $"{FormatCompactNumber(value)} {plural}";
    }

    private string FormatCompactCurrency(decimal value) =>
        $"{regional.CurrencySymbol}{FormatCompactValue(value)}";

    private string FormatCompactNumber(int value) =>
        FormatCompactValue(value);

    private static KpiTrendDisplayItem BuildTrend(decimal current, decimal previous, string comparisonContext, bool higherIsBetter = true)
    {
        var delta = current - previous;
        if (delta == 0)
            return new KpiTrendDisplayItem("→", $"Flat vs {comparisonContext}", KpiTrendTone.Neutral);

        var glyph = delta > 0 ? "↑" : "↓";
        var tone = delta > 0
            ? (higherIsBetter ? KpiTrendTone.Positive : KpiTrendTone.Negative)
            : (higherIsBetter ? KpiTrendTone.Negative : KpiTrendTone.Positive);

        if (previous == 0)
            return new KpiTrendDisplayItem(glyph, $"New vs {comparisonContext}", tone);

        var percentChange = Math.Abs(delta) / Math.Abs(previous) * 100m;
        return new KpiTrendDisplayItem(glyph, $"{percentChange:0.#}% vs {comparisonContext}", tone);
    }

    private static string FormatCompactValue(decimal value)
    {
        var absolute = Math.Abs(value);
        var divisor = 1m;
        var suffix = string.Empty;

        if (absolute >= 10000000m)
        {
            divisor = 10000000m;
            suffix = "Cr";
        }
        else if (absolute >= 100000m)
        {
            divisor = 100000m;
            suffix = "L";
        }
        else if (absolute >= 1000m)
        {
            divisor = 1000m;
            suffix = "K";
        }

        var compact = value / divisor;
        if (suffix.Length > 0 && Math.Abs(compact) < 100m)
            compact = Math.Truncate(compact * 10m) / 10m;

        var format = suffix.Length == 0 || Math.Abs(compact) >= 100m ? "0" : "0.#";
        return compact.ToString(format, CultureInfo.InvariantCulture) + suffix;
    }
}
