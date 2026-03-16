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
    public decimal ThisMonthSales { get; init; }
    public int ThisMonthTransactions { get; init; }

    // ── Inventory KPIs ──
    public int TotalProducts { get; init; }
    public int LowStockCount { get; init; }
    public int OutOfStockCount { get; init; }

    // ── Orders & Receivables ──
    public int PendingOrdersCount { get; init; }
    public int OverdueOrdersCount { get; init; }
    public decimal OutstandingReceivables { get; init; }

    // ── Recent sales (top 10 today) ──
    public IReadOnlyList<RecentSaleItem> RecentSales { get; init; } = [];

    // ── Top selling products (this month) ──
    public IReadOnlyList<TopProductItem> TopProducts { get; init; } = [];
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
