namespace StoreAssistantPro.Models.Mobile;

/// <summary>
/// Mobile dashboard summary data sent to companion app.
/// </summary>
public sealed record MobileDashboardData(
    decimal TodaySales,
    int TodayTransactions,
    decimal WeekSales,
    int LowStockCount,
    int PendingOrdersCount,
    DateTime LastSyncAt);

/// <summary>
/// Stock check result for mobile lookup.
/// </summary>
public sealed record MobileStockInfo(
    int ProductId,
    string ProductName,
    string? Barcode,
    decimal CurrentStock,
    decimal MinStockLevel,
    decimal SalePrice,
    string StockStatus);

/// <summary>
/// Mobile sales summary for daily/weekly view.
/// </summary>
public sealed record MobileSalesSummary(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal TotalSales,
    int TransactionCount,
    decimal AverageTicket,
    IReadOnlyList<MobileSalesByHour> HourlyBreakdown);

/// <summary>
/// Hourly sales breakdown for mobile chart display.
/// </summary>
public sealed record MobileSalesByHour(
    int Hour,
    decimal Amount,
    int Count);

/// <summary>
/// Mobile barcode scan result with product details.
/// </summary>
public sealed record MobileBarcodeLookup(
    bool Found,
    int? ProductId,
    string? ProductName,
    string? Barcode,
    decimal? SalePrice,
    decimal? Stock,
    string? CategoryName);

/// <summary>
/// Mobile customer lookup result.
/// </summary>
public sealed record MobileCustomerInfo(
    int CustomerId,
    string Name,
    string? Phone,
    string? Address,
    decimal TotalPurchases,
    DateTime? LastPurchaseDate);

/// <summary>
/// Mobile quick sale line item.
/// </summary>
public sealed class MobileQuickSaleItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
}

/// <summary>
/// Mobile quick sale request.
/// </summary>
public sealed class MobileQuickSaleRequest
{
    public List<MobileQuickSaleItem> Items { get; set; } = [];
    public int? CustomerId { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? StaffId { get; set; }
}

/// <summary>
/// Sync state for mobile offline mode.
/// </summary>
public sealed record MobileSyncState(
    DateTime LastSyncAt,
    int PendingUploadCount,
    int PendingDownloadCount,
    bool IsSyncing,
    string? LastError);

/// <summary>
/// Low stock alert for mobile push notification.
/// </summary>
public sealed record MobileLowStockAlert(
    int ProductId,
    string ProductName,
    decimal CurrentStock,
    decimal MinStockLevel,
    DateTime AlertedAt);

/// <summary>
/// Mobile daily report data.
/// </summary>
public sealed record MobileDailyReport(
    DateTime ReportDate,
    decimal TotalSales,
    decimal TotalExpenses,
    decimal NetRevenue,
    int TransactionCount,
    int NewCustomers,
    int ItemsSold,
    decimal TopSellerAmount,
    string? TopSellerName);
