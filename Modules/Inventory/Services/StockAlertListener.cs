using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Sales.Events;

namespace StoreAssistantPro.Modules.Inventory.Services;

/// <summary>
/// Listens for <see cref="SaleCompletedEvent"/> and checks if any sold products
/// have triggered stock alerts. Publishes notifications via <see cref="INotificationService"/>.
/// </summary>
public class StockAlertListener
{
    private readonly IEventBus _eventBus;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly INotificationService _notifications;
    private readonly ILogger<StockAlertListener> _logger;

    public StockAlertListener(
        IEventBus eventBus,
        IDbContextFactory<AppDbContext> contextFactory,
        INotificationService notifications,
        ILogger<StockAlertListener> logger)
    {
        _eventBus = eventBus;
        _contextFactory = contextFactory;
        _notifications = notifications;
        _logger = logger;

        _eventBus.Subscribe<SaleCompletedEvent>(OnSaleCompletedAsync);
    }

    private async Task OnSaleCompletedAsync(SaleCompletedEvent e)
    {
        try
        {
            await using var db = await _contextFactory.CreateDbContextAsync();
            var saleItems = await db.SaleItems.AsNoTracking()
                .Where(si => si.SaleId == e.SaleId)
                .Select(si => si.ProductId)
                .ToListAsync();

            var alerts = await db.StockAlerts.AsNoTracking()
                .Include(a => a.Product)
                .Where(a => a.IsEnabled && saleItems.Contains(a.ProductId))
                .ToListAsync();

            foreach (var alert in alerts)
            {
                if (alert.Product is null) continue;

                if (alert.Product.Quantity <= alert.LowThreshold)
                {
                    await _notifications.PostAsync(
                        $"Low Stock: {alert.Product.Name}",
                        $"Only {alert.Product.Quantity} left (threshold: {alert.LowThreshold})",
                        AppNotificationLevel.Warning);

                    _logger.LogWarning("Stock alert triggered for {Product}: {Qty} <= {Threshold}",
                        alert.Product.Name, alert.Product.Quantity, alert.LowThreshold);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check stock alerts after sale {SaleId}", e.SaleId);
        }
    }
}
