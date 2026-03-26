using StoreAssistantPro.Models.Mobile;

namespace StoreAssistantPro.Modules.Mobile.Services;

/// <summary>
/// Mobile companion service — provides data for the mobile app (#707-717).
/// Features: dashboard, stock check, sales summary, barcode scan,
/// price lookup, low stock alerts, daily report, customer lookup,
/// quick sale, offline sync, push notifications.
/// </summary>
public interface IMobileCompanionService
{
    /// <summary>Get mobile dashboard summary data (#707).</summary>
    Task<MobileDashboardData> GetDashboardAsync(CancellationToken ct = default);

    /// <summary>Check stock level for a product (#708).</summary>
    Task<MobileStockInfo?> CheckStockAsync(int productId, CancellationToken ct = default);

    /// <summary>Search products for stock check (#708).</summary>
    Task<IReadOnlyList<MobileStockInfo>> SearchStockAsync(string query, int maxResults = 20, CancellationToken ct = default);

    /// <summary>Get sales summary for a period (#709).</summary>
    Task<MobileSalesSummary> GetSalesSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Look up product by barcode scan (#710, #711).</summary>
    Task<MobileBarcodeLookup> LookupBarcodeAsync(string barcode, CancellationToken ct = default);

    /// <summary>Get low stock products for alerts (#712).</summary>
    Task<IReadOnlyList<MobileLowStockAlert>> GetLowStockAlertsAsync(CancellationToken ct = default);

    /// <summary>Get daily report data (#713).</summary>
    Task<MobileDailyReport> GetDailyReportAsync(DateTime date, CancellationToken ct = default);

    /// <summary>Look up customer by phone (#714).</summary>
    Task<MobileCustomerInfo?> LookupCustomerAsync(string phone, CancellationToken ct = default);

    /// <summary>Process a quick sale from mobile device (#715).</summary>
    Task<int> ProcessQuickSaleAsync(MobileQuickSaleRequest request, CancellationToken ct = default);

    /// <summary>Get current sync state (#716).</summary>
    Task<MobileSyncState> GetSyncStateAsync(CancellationToken ct = default);

    /// <summary>Trigger sync of offline data (#716).</summary>
    Task<MobileSyncState> SyncOfflineDataAsync(CancellationToken ct = default);

    /// <summary>Register device for push notifications (#717).</summary>
    Task RegisterDeviceAsync(string deviceToken, string platform, CancellationToken ct = default);

    /// <summary>Unregister device from push notifications (#717).</summary>
    Task UnregisterDeviceAsync(string deviceToken, CancellationToken ct = default);
}
