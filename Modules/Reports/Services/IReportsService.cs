using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Reports.Services;

public interface IReportsService
{
    void InvalidateCache();
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

    /// <summary>Category-wise sales report (#258).</summary>
    Task<IReadOnlyList<CategorySalesSummary>> GetCategorySalesReportAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Brand-wise sales report (#259).</summary>
    Task<IReadOnlyList<BrandSalesSummary>> GetBrandSalesReportAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Gross profit report for a period (#261).</summary>
    Task<GrossProfitReport> GetGrossProfitReportAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Net profit: gross profit minus expenses (#262).</summary>
    Task<NetProfitReport> GetNetProfitReportAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Top N best-selling products by quantity or revenue (#263).</summary>
    Task<IReadOnlyList<ProductSalesSummary>> GetBestSellingProductsAsync(DateTime from, DateTime to, int topN = 10, CancellationToken ct = default);

    /// <summary>Bottom N slow-moving products by quantity sold (#264).</summary>
    Task<IReadOnlyList<ProductSalesSummary>> GetSlowMovingProductsAsync(DateTime from, DateTime to, int topN = 10, CancellationToken ct = default);

    /// <summary>Sales grouped by cashier/user (#265).</summary>
    Task<IReadOnlyList<UserSalesSummary>> GetSalesByUserReportAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Sales grouped by payment method (#266).</summary>
    Task<IReadOnlyList<PaymentMethodSummary>> GetSalesByPaymentMethodAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Profit margin per product: (SalePrice - CostPrice) / SalePrice × 100 (#260).</summary>
    Task<IReadOnlyList<ProductMarginSummary>> GetProductMarginReportAsync(CancellationToken ct = default);

    /// <summary>Customer-wise sales report: revenue per customer sorted by spend (#268).</summary>
    Task<IReadOnlyList<CustomerSalesSummary>> GetCustomerSalesReportAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Hourly sales distribution for a date (#267).</summary>
    Task<IReadOnlyList<HourlySalesSummary>> GetSalesByHourAsync(DateTime date, CancellationToken ct = default);

    /// <summary>Daily return summary: aggregate return count and value per day (#151).</summary>
    Task<DailyReturnSummary> GetDailyReturnSummaryAsync(DateTime date, CancellationToken ct = default);

    /// <summary>Dead stock: products with zero sales in the date range (#80).</summary>
    Task<IReadOnlyList<DeadStockItem>> GetDeadStockReportAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Dashboard daily summary for printing (#409).</summary>
    Task<DashboardPrintData> GetDashboardPrintDataAsync(DateTime date, CancellationToken ct = default);
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

public record CategorySalesSummary(
    int? CategoryId,
    string CategoryName,
    int ProductCount,
    int TotalQuantitySold,
    decimal TotalRevenue,
    decimal TotalCost,
    decimal GrossProfit,
    decimal MarginPercent);

public record BrandSalesSummary(
    int? BrandId,
    string BrandName,
    int ProductCount,
    int TotalQuantitySold,
    decimal TotalRevenue,
    decimal TotalCost,
    decimal GrossProfit,
    decimal MarginPercent);

public record GrossProfitReport(
    DateTime From,
    DateTime To,
    decimal TotalRevenue,
    decimal TotalCostOfGoodsSold,
    decimal GrossProfit,
    decimal GrossMarginPercent,
    int SaleCount,
    int ItemsSold);

public record NetProfitReport(
    DateTime From,
    DateTime To,
    decimal TotalRevenue,
    decimal TotalCostOfGoodsSold,
    decimal GrossProfit,
    decimal TotalExpenses,
    decimal NetProfit,
    decimal NetMarginPercent);

public record UserSalesSummary(
    string CashierRole,
    int SaleCount,
    decimal TotalRevenue,
    decimal TotalDiscount,
    decimal AverageSaleValue);

public record PaymentMethodSummary(
    string PaymentMethod,
    int SaleCount,
    decimal TotalAmount,
    decimal Percentage);

public record ProductMarginSummary(
    int ProductId,
    string ProductName,
    string? CategoryName,
    string? BrandName,
    decimal SalePrice,
    decimal CostPrice,
    decimal Margin,
    decimal MarginPercent,
    int CurrentStock);

public record CustomerSalesSummary(
    int CustomerId,
    string CustomerName,
    string? Phone,
    int SaleCount,
    decimal TotalRevenue,
    decimal TotalDiscount,
    decimal AverageOrderValue);

public record HourlySalesSummary(
    int Hour,
    int SaleCount,
    decimal TotalRevenue,
    decimal AverageOrderValue);

public record DailyReturnSummary(
    DateTime Date,
    int ReturnCount,
    decimal TotalRefundAmount,
    int ItemsReturned);

public record Gstr3bSummaryRow(
    string Description,
    decimal TaxableValue,
    decimal Igst,
    decimal Cgst,
    decimal Sgst,
    decimal Cess);

public record DeadStockItem(
    int ProductId,
    string ProductName,
    string? CategoryName,
    string? BrandName,
    int CurrentStock,
    decimal CostPrice,
    decimal SalePrice,
    decimal StockValue);
