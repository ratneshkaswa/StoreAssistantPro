using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Inventory.Services;

public class StockAlertService(IDbContextFactory<AppDbContext> contextFactory) : IStockAlertService
{
    public async Task<List<StockAlert>> GetAllAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.StockAlerts.AsNoTracking()
            .Include(a => a.Product)
            .OrderBy(a => a.Product!.Name)
            .ToListAsync();
    }

    public async Task<StockAlert?> GetByProductIdAsync(int productId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.StockAlerts.AsNoTracking()
            .Include(a => a.Product)
            .FirstOrDefaultAsync(a => a.ProductId == productId);
    }

    public async Task<StockAlert> CreateOrUpdateAsync(StockAlert alert)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var existing = await db.StockAlerts.FirstOrDefaultAsync(a => a.ProductId == alert.ProductId);
        if (existing is not null)
        {
            existing.LowThreshold = alert.LowThreshold;
            existing.HighThreshold = alert.HighThreshold;
            existing.IsEnabled = alert.IsEnabled;
        }
        else
        {
            db.StockAlerts.Add(alert);
        }
        await db.SaveChangesAsync();
        return existing ?? alert;
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var entity = await db.StockAlerts.FindAsync(id);
        if (entity is not null)
        {
            db.StockAlerts.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<Product>> GetLowStockProductsAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.StockAlerts.AsNoTracking()
            .Where(a => a.IsEnabled && a.Product!.IsActive && a.Product.Quantity <= a.LowThreshold)
            .Select(a => a.Product!)
            .OrderBy(p => p.Quantity)
            .ToListAsync();
    }

    public async Task<List<Product>> GetOverStockProductsAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.StockAlerts.AsNoTracking()
            .Where(a => a.IsEnabled && a.HighThreshold > 0 && a.Product!.IsActive && a.Product.Quantity > a.HighThreshold)
            .Select(a => a.Product!)
            .OrderByDescending(p => p.Quantity)
            .ToListAsync();
    }
}
