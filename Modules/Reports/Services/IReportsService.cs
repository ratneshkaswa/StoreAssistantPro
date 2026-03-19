using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Reports.Services;

public interface IReportsService
{
    Task<ExpenseReport> GetExpenseReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IroningReport> GetIroningReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<OrderReport> GetOrderReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<InwardReport> GetInwardReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<DebtorReport> GetDebtorReportAsync(CancellationToken ct = default);

    // ── Sales & Tax Reports ──

    /// <summary>Daily sales summary: total sales, returns, net, count (#255).</summary>
    Task<DailySalesSummary> GetDailySalesSummaryAsync(DateTime date, CancellationToken ct = default);

    /// <summary>Monthly sales summary with revenue, COGS, gross profit (#256).</summary>
    Task<MonthlySalesSummary> GetMonthlySalesSummaryAsync(int year, int month, CancellationToken ct = default);

    /// <summary>HSN-wise tax summary for GST compliance (#132/#196).</summary>
    Task<IReadOnlyList<HsnTaxSummaryLine>> GetHsnTaxSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Monthly tax report: CGST + SGST + IGST breakdown (#205).</summary>
    Task<TaxReport> GetTaxReportAsync(int year, int month, CancellationToken ct = default);

    /// <summary>Daily discount report: total discount value given (#188).</summary>
    Task<DailyDiscountReport> GetDailyDiscountReportAsync(DateTime date, CancellationToken ct = default);

    /// <summary>Discount history log: every discount given with details (#187).</summary>
    Task<IReadOnlyList<DiscountHistoryEntry>> GetDiscountHistoryAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Product-wise sales report for a date range (#257).</summary>
    Task<IReadOnlyList<ProductSalesSummary>> GetProductSalesReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

public record ExpenseReport(
    int Count,
    decimal Total,
    IReadOnlyList<CategoryBreakdown> ByCategory,
    IReadOnlyList<MonthlyTotal> MonthlyTrend,
    IReadOnlyList<Expense> RecentEntries);

public record CategoryBreakdown(string Category, decimal Amount);

public record MonthlyTotal(string Month, decimal Amount);

public record IroningReport(
    int Count,
    decimal Total,
    decimal PaidTotal,
    decimal UnpaidTotal,
    IReadOnlyList<IroningEntry> RecentEntries);

public record OrderReport(
    int Count,
    decimal Total,
    int Delivered,
    int Pending,
    IReadOnlyList<Order> RecentEntries);

public record InwardReport(
    int Count,
    decimal Total,
    IReadOnlyList<InwardEntry> RecentEntries);

public record DebtorReport(
    int Count,
    decimal TotalOutstanding,
    IReadOnlyList<TopDebtor> TopDebtors);

public record TopDebtor(string Name, decimal Balance);

// ── Sales & Tax Report Records ──

public record DailySalesSummary(
    DateTime Date,
    int SaleCount,
    decimal TotalSales,
    decimal TotalReturns,
    decimal NetSales,
    decimal TotalTax,
    decimal TotalDiscount);

public record MonthlySalesSummary(
    int Year,
    int Month,
    int SaleCount,
    decimal Revenue,
    decimal CostOfGoodsSold,
    decimal GrossProfit,
    decimal TotalTax,
    decimal TotalDiscount);

public record HsnTaxSummaryLine(
    string HsnCode,
    decimal TaxableValue,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal IgstAmount,
    decimal TotalTax,
    decimal CgstRate,
    decimal SgstRate);

public record TaxReport(
    int Year,
    int Month,
    decimal TotalTaxCollected,
    decimal TotalCgst,
    decimal TotalSgst,
    decimal TotalIgst,
    IReadOnlyList<HsnTaxSummaryLine> HsnBreakdown);

public record DailyDiscountReport(
    DateTime Date,
    int DiscountedSaleCount,
    decimal TotalDiscountAmount,
    decimal AverageDiscountPercent);

public record DiscountHistoryEntry(
    int SaleId,
    string InvoiceNumber,
    DateTime SaleDate,
    string DiscountType,
    decimal DiscountValue,
    decimal DiscountAmount,
    string? DiscountReason,
    string? CashierRole);

public record ProductSalesSummary(
    int ProductId,
    string ProductName,
    string? HsnCode,
    int TotalQuantitySold,
    decimal TotalRevenue,
    decimal TotalTax,
    decimal TotalDiscount,
    decimal AverageSellingPrice);
