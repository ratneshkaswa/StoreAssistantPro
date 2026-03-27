namespace StoreAssistantPro.Modules.MainShell.Models;

/// <summary>
/// Module-local DTO carrying aggregated dashboard metrics.
/// </summary>
public sealed record DashboardSummary
{
    public static readonly DashboardSummary Empty = new();

    // ── Sales KPIs ──
    public decimal TodaySales { get; init; }
    public int TodayTransactions { get; init; }
    public decimal TodayReturns { get; init; }
    public decimal TodayNetSales { get; init; }
    public decimal TodayProfit { get; init; }
    public decimal AverageBillValue { get; init; }
    public decimal ThisMonthSales { get; init; }
    public int ThisMonthTransactions { get; init; }
    public decimal PreviousDaySales { get; init; }
    public decimal PreviousDayReturns { get; init; }
    public decimal PreviousDayNetSales { get; init; }
    public decimal PreviousDayAverageBillValue { get; init; }
    public decimal PreviousMonthSales { get; init; }

    // ── Inventory KPIs ──
    public int TotalProducts { get; init; }
    public int LowStockCount { get; init; }
    public int OutOfStockCount { get; init; }

    // ── Orders & Receivables ──
    public int PendingOrdersCount { get; init; }
    public int OverdueOrdersCount { get; init; }
    public decimal OutstandingReceivables { get; init; }
    public int PendingPaymentsCount { get; init; }

    // ── Recent sales (top 10 today) ──
    public IReadOnlyList<RecentSaleItem> RecentSales { get; init; } = [];

    // ── Top selling products (today) ──
    public IReadOnlyList<TopProductItem> TopProductsToday { get; init; } = [];

    // ── Monthly sales trend (#398) ──
    public IReadOnlyList<DailySalesTrendItem> DailySalesTrend { get; init; } = [];

    // ── Backup status (#332) ──
    public DateTime? LastBackupDate { get; init; }

    // ── Monthly expense trend (#399) ──
    public IReadOnlyList<DailyExpenseTrendItem> DailyExpenseTrend { get; init; } = [];

    // ── Category sales breakdown (#400) ──
    public IReadOnlyList<CategorySalesBreakdownItem> CategorySalesBreakdown { get; init; } = [];

    // ── Year-over-year comparison (#402) ──
    public decimal SameMonthLastYearSales { get; init; }

    // ── Sales target (#403) ──
    public decimal MonthlySalesTarget { get; init; }

    // ── Upcoming tasks (#406) ──
    public int PendingPurchaseOrdersCount { get; init; }
    public int OverduePaymentsCount { get; init; }
    public bool BackupOverdue { get; init; }
}

/// <summary>Lightweight projection of a recent sale for the dashboard grid.</summary>
public sealed record RecentSaleItem(
    string InvoiceNumber,
    DateTime SaleDate,
    decimal TotalAmount,
    string PaymentMethod,
    int ItemCount);

/// <summary>Lightweight projection of a top-selling product for the dashboard.</summary>
public sealed record TopProductItem(
    string ProductName,
    int QuantitySold,
    decimal Revenue);

/// <summary>Daily sales total for trend chart (#398).</summary>
public sealed record DailySalesTrendItem(
    DateTime Date,
    decimal TotalSales,
    int TransactionCount);

/// <summary>Daily expense total for trend chart (#399).</summary>
public sealed record DailyExpenseTrendItem(
    DateTime Date,
    decimal TotalExpenses,
    int ExpenseCount);

/// <summary>Category sales breakdown for pie chart (#400).</summary>
public sealed record CategorySalesBreakdownItem(
    string CategoryName,
    decimal Revenue,
    int ItemCount);
