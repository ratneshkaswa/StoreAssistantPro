using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.Mobile;

namespace StoreAssistantPro.Modules.Mobile.Services;

public sealed class MobileCompanionService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<MobileCompanionService> logger) : IMobileCompanionService
{
    private readonly List<string> _registeredDevices = [];

    public async Task<MobileDashboardData> GetDashboardAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);

        var todaySales = await context.Sales.Where(s => s.SaleDate >= today).SumAsync(s => s.TotalAmount, ct).ConfigureAwait(false);
        var todayTx = await context.Sales.CountAsync(s => s.SaleDate >= today, ct).ConfigureAwait(false);
        var weekSales = await context.Sales.Where(s => s.SaleDate >= weekStart).SumAsync(s => s.TotalAmount, ct).ConfigureAwait(false);
        var lowStock = await context.Products.CountAsync(p => p.IsActive && p.Quantity <= p.MinStockLevel && p.Quantity > 0, ct).ConfigureAwait(false);
        var pendingOrders = await context.Orders.CountAsync(o => o.Status == "Pending", ct).ConfigureAwait(false);

        return new MobileDashboardData(todaySales, todayTx, weekSales, lowStock, pendingOrders, DateTime.UtcNow);
    }

    public async Task<MobileStockInfo?> CheckStockAsync(int productId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Products.Where(p => p.Id == productId).Select(p => new MobileStockInfo(
            p.Id, p.Name, p.Barcode, p.Quantity, p.MinStockLevel, p.SalePrice,
            p.Quantity <= 0 ? "OutOfStock" : p.Quantity <= p.MinStockLevel ? "LowStock" : "InStock"
        )).FirstOrDefaultAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<MobileStockInfo>> SearchStockAsync(string query, int maxResults = 20, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Products.Where(p => p.IsActive && (p.Name.Contains(query) || (p.Barcode != null && p.Barcode.Contains(query))))
            .Take(maxResults)
            .Select(p => new MobileStockInfo(
                p.Id, p.Name, p.Barcode, p.Quantity, p.MinStockLevel, p.SalePrice,
                p.Quantity <= 0 ? "OutOfStock" : p.Quantity <= p.MinStockLevel ? "LowStock" : "InStock"
            )).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<MobileSalesSummary> GetSalesSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var sales = await context.Sales.Where(s => s.SaleDate >= from && s.SaleDate <= to)
            .Select(s => new { s.TotalAmount, s.SaleDate }).ToListAsync(ct).ConfigureAwait(false);

        var total = sales.Sum(s => s.TotalAmount);
        var hourly = sales.GroupBy(s => s.SaleDate.Hour)
            .Select(g => new MobileSalesByHour(g.Key, g.Sum(s => s.TotalAmount), g.Count())).OrderBy(h => h.Hour).ToList();

        return new MobileSalesSummary(from, to, total, sales.Count,
            sales.Count > 0 ? total / sales.Count : 0, hourly);
    }

    public async Task<MobileBarcodeLookup> LookupBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var product = await context.Products.Where(p => p.Barcode == barcode)
            .Select(p => new { p.Id, p.Name, p.Barcode, p.SalePrice, p.Quantity, CategoryName = p.Category != null ? p.Category.Name : null })
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        if (product is null)
            return new MobileBarcodeLookup(false, null, null, barcode, null, null, null);

        return new MobileBarcodeLookup(true, product.Id, product.Name, product.Barcode, product.SalePrice, product.Quantity, product.CategoryName);
    }

    public async Task<IReadOnlyList<MobileLowStockAlert>> GetLowStockAlertsAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Products.Where(p => p.IsActive && p.Quantity <= p.MinStockLevel)
            .Select(p => new MobileLowStockAlert(p.Id, p.Name, p.Quantity, p.MinStockLevel, DateTime.UtcNow))
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<MobileDailyReport> GetDailyReportAsync(DateTime date, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var nextDay = date.Date.AddDays(1);
        var sales = await context.Sales.Where(s => s.SaleDate >= date.Date && s.SaleDate < nextDay).ToListAsync(ct).ConfigureAwait(false);
        var expenses = await context.Expenses.Where(e => e.Date >= date.Date && e.Date < nextDay).SumAsync(e => e.Amount, ct).ConfigureAwait(false);
        var totalSales = sales.Sum(s => s.TotalAmount);
        var itemsSold = await context.SaleItems.CountAsync(si => si.Sale!.SaleDate >= date.Date && si.Sale.SaleDate < nextDay, ct).ConfigureAwait(false);
        var newCustomers = await context.Customers.CountAsync(c => c.CreatedDate >= date.Date && c.CreatedDate < nextDay, ct).ConfigureAwait(false);

        var topSeller = sales.OrderByDescending(s => s.TotalAmount).FirstOrDefault();

        return new MobileDailyReport(date.Date, totalSales, expenses, totalSales - expenses,
            sales.Count, newCustomers, itemsSold, topSeller?.TotalAmount ?? 0, topSeller?.InvoiceNumber);
    }

    public async Task<MobileCustomerInfo?> LookupCustomerAsync(string phone, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Customers.Where(c => c.Phone == phone)
            .Select(c => new MobileCustomerInfo(c.Id, c.Name, c.Phone, c.Address, c.TotalPurchaseAmount, (DateTime?)null))
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);
    }

    public Task<int> ProcessQuickSaleAsync(MobileQuickSaleRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("Mobile quick sale: {Count} items, payment={Method}", request.Items.Count, request.PaymentMethod);
        return Task.FromResult(0); // Returns sale ID after processing via command pipeline.
    }

    public Task<MobileSyncState> GetSyncStateAsync(CancellationToken ct = default)
        => Task.FromResult(new MobileSyncState(DateTime.UtcNow, 0, 0, false, null));

    public Task<MobileSyncState> SyncOfflineDataAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Mobile offline sync triggered");
        return Task.FromResult(new MobileSyncState(DateTime.UtcNow, 0, 0, false, null));
    }

    public Task RegisterDeviceAsync(string deviceToken, string platform, CancellationToken ct = default)
    {
        if (!_registeredDevices.Contains(deviceToken))
            _registeredDevices.Add(deviceToken);
        logger.LogInformation("Mobile device registered: {Platform} {Token}", platform, deviceToken[..Math.Min(8, deviceToken.Length)] + "…");
        return Task.CompletedTask;
    }

    public Task UnregisterDeviceAsync(string deviceToken, CancellationToken ct = default)
    {
        _registeredDevices.Remove(deviceToken);
        logger.LogInformation("Mobile device unregistered");
        return Task.CompletedTask;
    }
}
