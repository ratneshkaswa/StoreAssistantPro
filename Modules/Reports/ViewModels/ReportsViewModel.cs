using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Printing;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Reports.Services;

namespace StoreAssistantPro.Modules.Reports.ViewModels;

public partial class ReportsViewModel(
    IReportsService reportsService,
    IPrintReportService printReportService,
    IPrintPreviewService printPreviewService,
    IAuditService auditService) : BaseViewModel
{
    // ── Date range ──

    [ObservableProperty]
    public partial DateTime DateFrom { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    public partial DateTime DateTo { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial string ActivePreset { get; set; } = "This Month";

    // ── Daily sales summary (#255) ──

    [ObservableProperty]
    public partial int TodaySaleCount { get; set; }

    [ObservableProperty]
    public partial decimal TodayTotalSales { get; set; }

    [ObservableProperty]
    public partial decimal TodayTotalReturns { get; set; }

    [ObservableProperty]
    public partial decimal TodayNetSales { get; set; }

    [ObservableProperty]
    public partial decimal TodayTotalTax { get; set; }

    [ObservableProperty]
    public partial decimal TodayTotalDiscount { get; set; }

    // ── Gross profit (#261) ──

    [ObservableProperty]
    public partial decimal GrossRevenue { get; set; }

    [ObservableProperty]
    public partial decimal GrossCogs { get; set; }

    [ObservableProperty]
    public partial decimal GrossProfit { get; set; }

    [ObservableProperty]
    public partial decimal GrossMarginPercent { get; set; }

    [ObservableProperty]
    public partial int PeriodSaleCount { get; set; }

    [ObservableProperty]
    public partial int PeriodItemsSold { get; set; }

    // ── Net profit (#262) ──

    [ObservableProperty]
    public partial decimal NetProfit { get; set; }

    [ObservableProperty]
    public partial decimal NetMarginPercent { get; set; }

    [ObservableProperty]
    public partial decimal TotalExpensesForProfit { get; set; }

    // ── Tax summary (#205) ──

    [ObservableProperty]
    public partial decimal TotalTaxCollected { get; set; }

    [ObservableProperty]
    public partial decimal TotalCgst { get; set; }

    [ObservableProperty]
    public partial decimal TotalSgst { get; set; }

    // ── Best / slow selling products (#263/#264) ──

    [ObservableProperty]
    public partial ObservableCollection<ProductSalesSummary> BestSellingProducts { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ProductSalesSummary> SlowMovingProducts { get; set; } = [];

    // ── Sales by payment method (#266) ──

    [ObservableProperty]
    public partial ObservableCollection<PaymentMethodSummary> SalesByPaymentMethod { get; set; } = [];

    // ── Sales by user (#265) ──

    [ObservableProperty]
    public partial ObservableCollection<UserSalesSummary> SalesByUser { get; set; } = [];

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

    // ── On-demand report exports (#270) ──

    [RelayCommand]
    private Task ExportProductSalesCsvAsync() => RunAsync(async ct =>
    {
        var data = await reportsService.GetProductSalesReportAsync(DateFrom, DateTo, ct);
        if (data.Count == 0) { ErrorMessage = "No data to export."; return; }
        if (CsvExporter.Export(data, $"ProductSales_{DateFrom:yyyyMMdd}_{DateTo:yyyyMMdd}.csv"))
            SuccessMessage = "Product sales report exported.";
    });

    [RelayCommand]
    private Task ExportCategorySalesCsvAsync() => RunAsync(async ct =>
    {
        var data = await reportsService.GetCategorySalesReportAsync(DateFrom, DateTo, ct);
        if (data.Count == 0) { ErrorMessage = "No data to export."; return; }
        if (CsvExporter.Export(data, $"CategorySales_{DateFrom:yyyyMMdd}_{DateTo:yyyyMMdd}.csv"))
            SuccessMessage = "Category sales report exported.";
    });

    [RelayCommand]
    private Task ExportBrandSalesCsvAsync() => RunAsync(async ct =>
    {
        var data = await reportsService.GetBrandSalesReportAsync(DateFrom, DateTo, ct);
        if (data.Count == 0) { ErrorMessage = "No data to export."; return; }
        if (CsvExporter.Export(data, $"BrandSales_{DateFrom:yyyyMMdd}_{DateTo:yyyyMMdd}.csv"))
            SuccessMessage = "Brand sales report exported.";
    });

    [RelayCommand]
    private Task ExportDiscountHistoryCsvAsync() => RunAsync(async ct =>
    {
        var data = await reportsService.GetDiscountHistoryAsync(DateFrom, DateTo, ct);
        if (data.Count == 0) { ErrorMessage = "No data to export."; return; }
        if (CsvExporter.Export(data, $"DiscountHistory_{DateFrom:yyyyMMdd}_{DateTo:yyyyMMdd}.csv"))
            SuccessMessage = "Discount history exported.";
    });

    [RelayCommand]
    private Task ExportHsnTaxCsvAsync() => RunAsync(async ct =>
    {
        var data = await reportsService.GetHsnTaxSummaryAsync(DateFrom, DateTo, ct);
        if (data.Count == 0) { ErrorMessage = "No data to export."; return; }
        if (CsvExporter.Export(data, $"HsnTaxSummary_{DateFrom:yyyyMMdd}_{DateTo:yyyyMMdd}.csv"))
            SuccessMessage = "HSN tax summary exported.";
    });

    [RelayCommand]
    private Task ExportProductMarginCsvAsync() => RunAsync(async ct =>
    {
        var data = await reportsService.GetProductMarginReportAsync(ct);
        if (data.Count == 0) { ErrorMessage = "No data to export."; return; }
        if (CsvExporter.Export(data, $"ProductMargins_{DateTime.Today:yyyyMMdd}.csv"))
            SuccessMessage = "Product margin report exported.";
    });

    [RelayCommand]
    private Task ExportSalesByUserCsvAsync() => RunAsync(async ct =>
    {
        var data = await reportsService.GetSalesByUserReportAsync(DateFrom, DateTo, ct);
        if (data.Count == 0) { ErrorMessage = "No data to export."; return; }
        if (CsvExporter.Export(data, $"SalesByUser_{DateFrom:yyyyMMdd}_{DateTo:yyyyMMdd}.csv"))
            SuccessMessage = "Sales by user report exported.";
    });

    [RelayCommand]
    private Task ExportSalesByPaymentMethodCsvAsync() => RunAsync(async ct =>
    {
        var data = await reportsService.GetSalesByPaymentMethodAsync(DateFrom, DateTo, ct);
        if (data.Count == 0) { ErrorMessage = "No data to export."; return; }
        if (CsvExporter.Export(data, $"SalesByPayment_{DateFrom:yyyyMMdd}_{DateTo:yyyyMMdd}.csv"))
            SuccessMessage = "Sales by payment method exported.";
    });

    [RelayCommand]
    private Task ExportBestSellingCsvAsync() => RunAsync(async ct =>
    {
        var data = await reportsService.GetBestSellingProductsAsync(DateFrom, DateTo, 50, ct);
        if (data.Count == 0) { ErrorMessage = "No data to export."; return; }
        if (CsvExporter.Export(data, $"BestSelling_{DateFrom:yyyyMMdd}_{DateTo:yyyyMMdd}.csv"))
            SuccessMessage = "Best selling products exported.";
    });

    [RelayCommand]
    private Task ExportSlowMovingCsvAsync() => RunAsync(async ct =>
    {
        var data = await reportsService.GetSlowMovingProductsAsync(DateFrom, DateTo, 50, ct);
        if (data.Count == 0) { ErrorMessage = "No data to export."; return; }
        if (CsvExporter.Export(data, $"SlowMoving_{DateFrom:yyyyMMdd}_{DateTo:yyyyMMdd}.csv"))
            SuccessMessage = "Slow moving products exported.";
    });

    // ── Print commands (#447-#449) ──

    [RelayCommand]
    private Task PrintDailyReportAsync() => RunAsync(async ct =>
    {
        ClearMessages();
        var text = await printReportService.GenerateDailySalesReportAsync(DateTime.Today, ct);
        if (ReportPrintHelper.PrintReport(text, "Daily Sales Report"))
            SuccessMessage = "Daily sales report sent to printer.";
    });

    [RelayCommand]
    private Task PrintMonthlyReportAsync() => RunAsync(async ct =>
    {
        ClearMessages();
        var text = await printReportService.GenerateMonthlySalesReportAsync(DateFrom.Year, DateFrom.Month, ct);
        if (ReportPrintHelper.PrintReport(text, "Monthly Sales Report"))
            SuccessMessage = "Monthly sales report sent to printer.";
    });

    [RelayCommand]
    private Task PrintStockReportAsync() => RunAsync(async ct =>
    {
        ClearMessages();
        var text = await printReportService.GenerateStockReportAsync(ct);
        if (ReportPrintHelper.PrintReport(text, "Stock Report"))
            SuccessMessage = "Stock report sent to printer.";
    });

    // ── Preview commands (#454) ──

    [RelayCommand]
    private Task PreviewDailyReportAsync() => RunAsync(async ct =>
    {
        ClearMessages();
        var text = await printReportService.GenerateDailySalesReportAsync(DateTime.Today, ct);
        printPreviewService.ShowPreview(text, "Daily Sales Report");
    });

    [RelayCommand]
    private Task PreviewMonthlyReportAsync() => RunAsync(async ct =>
    {
        ClearMessages();
        var text = await printReportService.GenerateMonthlySalesReportAsync(DateFrom.Year, DateFrom.Month, ct);
        printPreviewService.ShowPreview(text, "Monthly Sales Report");
    });

    [RelayCommand]
    private Task PreviewStockReportAsync() => RunAsync(async ct =>
    {
        ClearMessages();
        var text = await printReportService.GenerateStockReportAsync(ct);
        printPreviewService.ShowPreview(text, "Stock Report");
    });

    // ── Audit log export (#300) ──

    [RelayCommand]
    private Task ExportAuditLogCsvAsync() => RunAsync(async ct =>
    {
        var logs = await auditService.GetLogsAsync(
            from: DateFrom, to: DateTo.Date.AddDays(1).AddTicks(-1),
            maxResults: 10000, ct: ct);
        if (logs.Count == 0) { ErrorMessage = "No audit entries for the selected period."; return; }
        if (CsvExporter.Export(logs, $"AuditLog_{DateFrom:yyyyMMdd}_{DateTo:yyyyMMdd}.csv"))
            SuccessMessage = $"Audit log exported ({logs.Count} entries).";
    });

    // ── GSTR-1 data export (#207) ──

    [RelayCommand]
    private Task ExportGstr1CsvAsync() => RunAsync(async ct =>
    {
        var from = DateFrom;
        var to = DateTo.Date.AddDays(1).AddTicks(-1);
        var hsn = await reportsService.GetHsnTaxSummaryAsync(from, to, ct);
        if (hsn.Count == 0) { ErrorMessage = "No HSN data for the selected period."; return; }
        if (CsvExporter.Export(hsn, $"GSTR1_HSN_{DateFrom:yyyyMMdd}_{DateTo:yyyyMMdd}.csv"))
            SuccessMessage = "GSTR-1 HSN summary exported.";
    });

    // ── GSTR-3B summary export (#208) ──

    [RelayCommand]
    private Task ExportGstr3bCsvAsync() => RunAsync(async ct =>
    {
        var from = DateFrom;
        var to = DateTo.Date.AddDays(1).AddTicks(-1);
        var tax = await reportsService.GetTaxReportAsync(from.Year, from.Month, ct);
        var taxableValue = tax.HsnBreakdown.Sum(h => h.TaxableValue);
        var rows = new[]
        {
            new Gstr3bSummaryRow("3.1(a) Outward taxable supplies", taxableValue,
                tax.TotalIgst, tax.TotalCgst, tax.TotalSgst, 0m),
            new Gstr3bSummaryRow("Total", taxableValue,
                tax.TotalIgst, tax.TotalCgst, tax.TotalSgst, 0m)
        };
        if (CsvExporter.Export(rows, $"GSTR3B_{from:yyyyMM}.csv"))
            SuccessMessage = "GSTR-3B summary exported.";
    });

    // ── Dead stock report (#80) ──

    [RelayCommand]
    private Task ExportDeadStockCsvAsync() => RunAsync(async ct =>
    {
        var from = DateFrom;
        var to = DateTo.Date.AddDays(1).AddTicks(-1);
        var data = await reportsService.GetDeadStockReportAsync(from, to, ct);
        if (data.Count == 0) { ErrorMessage = "No dead stock found for the selected period."; return; }
        if (CsvExporter.Export(data, $"DeadStock_{DateFrom:yyyyMMdd}_{DateTo:yyyyMMdd}.csv"))
            SuccessMessage = $"Dead stock report exported ({data.Count} products).";
    });

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    private async Task RefreshAllAsync(CancellationToken ct)
    {
        var from = DateFrom;
        var to = DateTo.Date.AddDays(1).AddTicks(-1);

        // ── Daily sales summary (today) ──
        var daily = await reportsService.GetDailySalesSummaryAsync(DateTime.Today, ct);
        TodaySaleCount = daily.SaleCount;
        TodayTotalSales = daily.TotalSales;
        TodayTotalReturns = daily.TotalReturns;
        TodayNetSales = daily.NetSales;
        TodayTotalTax = daily.TotalTax;
        TodayTotalDiscount = daily.TotalDiscount;

        // ── Gross profit (selected period) ──
        var gross = await reportsService.GetGrossProfitReportAsync(from, to, ct);
        GrossRevenue = gross.TotalRevenue;
        GrossCogs = gross.TotalCostOfGoodsSold;
        GrossProfit = gross.GrossProfit;
        GrossMarginPercent = gross.GrossMarginPercent;
        PeriodSaleCount = gross.SaleCount;
        PeriodItemsSold = gross.ItemsSold;

        // ── Net profit (selected period) ──
        var net = await reportsService.GetNetProfitReportAsync(from, to, ct);
        NetProfit = net.NetProfit;
        NetMarginPercent = net.NetMarginPercent;
        TotalExpensesForProfit = net.TotalExpenses;

        // ── Tax summary (current month) ──
        var today = DateTime.Today;
        var tax = await reportsService.GetTaxReportAsync(today.Year, today.Month, ct);
        TotalTaxCollected = tax.TotalTaxCollected;
        TotalCgst = tax.TotalCgst;
        TotalSgst = tax.TotalSgst;

        // ── Best / slow selling products ──
        var best = await reportsService.GetBestSellingProductsAsync(from, to, 10, ct);
        BestSellingProducts = new ObservableCollection<ProductSalesSummary>(best);

        var slow = await reportsService.GetSlowMovingProductsAsync(from, to, 10, ct);
        SlowMovingProducts = new ObservableCollection<ProductSalesSummary>(slow);

        // ── Sales by payment method ──
        var byPayment = await reportsService.GetSalesByPaymentMethodAsync(from, to, ct);
        SalesByPaymentMethod = new ObservableCollection<PaymentMethodSummary>(byPayment);

        // ── Sales by user ──
        var byUser = await reportsService.GetSalesByUserReportAsync(from, to, ct);
        SalesByUser = new ObservableCollection<UserSalesSummary>(byUser);

        // ── Expense report ──
        var expense = await reportsService.GetExpenseReportAsync(from, to, ct);
        ExpenseCount = expense.Count;
        ExpenseTotal = expense.Total;
        ExpenseByCategory = new ObservableCollection<CategoryBreakdown>(expense.ByCategory);
        ExpenseMonthlyTrend = new ObservableCollection<MonthlyTotal>(expense.MonthlyTrend);
        ExpenseRecent = new ObservableCollection<Expense>(expense.RecentEntries);

        // ── Ironing report ──
        var ironing = await reportsService.GetIroningReportAsync(from, to, ct);
        IroningCount = ironing.Count;
        IroningTotal = ironing.Total;
        IroningPaid = ironing.PaidTotal;
        IroningUnpaid = ironing.UnpaidTotal;
        IroningRecent = new ObservableCollection<IroningEntry>(ironing.RecentEntries);

        // ── Order report ──
        var order = await reportsService.GetOrderReportAsync(from, to, ct);
        OrderCount = order.Count;
        OrderTotal = order.Total;
        OrderDelivered = order.Delivered;
        OrderPending = order.Pending;
        OrderRecent = new ObservableCollection<Order>(order.RecentEntries);

        // ── Inward report ──
        var inward = await reportsService.GetInwardReportAsync(from, to, ct);
        InwardCount = inward.Count;
        InwardTotal = inward.Total;
        InwardRecent = new ObservableCollection<InwardEntry>(inward.RecentEntries);

        // ── Debtor report ──
        var debtor = await reportsService.GetDebtorReportAsync(ct);
        DebtorCount = debtor.Count;
        DebtorOutstanding = debtor.TotalOutstanding;
        TopDebtors = new ObservableCollection<TopDebtor>(debtor.TopDebtors);
    }
}
