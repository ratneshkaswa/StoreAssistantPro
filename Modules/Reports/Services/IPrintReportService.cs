namespace StoreAssistantPro.Modules.Reports.Services;

public interface IPrintReportService
{
    /// <summary>Generate printable daily sales report (#447).</summary>
    Task<string> GenerateDailySalesReportAsync(DateTime date, CancellationToken ct = default);

    /// <summary>Generate printable monthly sales report (#448).</summary>
    Task<string> GenerateMonthlySalesReportAsync(int year, int month, CancellationToken ct = default);

    /// <summary>Generate printable stock report (#449).</summary>
    Task<string> GenerateStockReportAsync(CancellationToken ct = default);

    /// <summary>Generate printable customer statement (#450).</summary>
    Task<string> GenerateCustomerStatementAsync(int customerId, DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Generate printable purchase order (#451).</summary>
    Task<string> GeneratePurchaseOrderPrintAsync(int purchaseOrderId, CancellationToken ct = default);
}
