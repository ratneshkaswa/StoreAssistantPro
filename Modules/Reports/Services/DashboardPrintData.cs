namespace StoreAssistantPro.Modules.Reports.Services;

/// <summary>Daily dashboard summary for printing (#409).</summary>
public record DashboardPrintData(
    DateTime Date,
    decimal TotalSales,
    decimal TotalReturns,
    decimal NetSales,
    int TransactionCount,
    decimal AverageBillValue,
    decimal TotalExpenses,
    decimal GrossProfit,
    IReadOnlyList<ProductSalesSummary> TopProducts,
    IReadOnlyList<PaymentMethodSummary> PaymentBreakdown);
