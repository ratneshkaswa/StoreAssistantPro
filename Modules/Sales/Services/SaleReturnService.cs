using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Sales.Services;

public class SaleReturnService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional) : ISaleReturnService
{
    public async Task<List<SaleReturn>> GetAllAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.SaleReturns.AsNoTracking()
            .Include(r => r.Sale)
            .Include(r => r.SaleItem)
                .ThenInclude(si => si!.Product)
            .OrderByDescending(r => r.ReturnDate)
            .ToListAsync();
    }

    public async Task<List<SaleReturn>> GetBySaleIdAsync(int saleId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.SaleReturns.AsNoTracking()
            .Where(r => r.SaleId == saleId)
            .Include(r => r.SaleItem)
                .ThenInclude(si => si!.Product)
            .ToListAsync();
    }

    public async Task<SaleReturn> ProcessReturnAsync(SaleReturn saleReturn)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();

        db.SaleReturns.Add(saleReturn);

        // Restore stock if flagged
        if (saleReturn.StockRestored)
        {
            var saleItem = await db.SaleItems
                .Include(si => si.Product)
                .FirstOrDefaultAsync(si => si.Id == saleReturn.SaleItemId);

            if (saleItem?.Product is not null)
            {
                saleItem.Product.Quantity += saleReturn.Quantity;
            }
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return saleReturn;
    }

    public async Task<string> GenerateReturnNumberAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var today = regional.Now;
        var datePrefix = today.ToString("yyyyMMdd");
        var todayReturns = await db.SaleReturns
            .Where(r => r.ReturnNumber.StartsWith($"RET-{datePrefix}"))
            .CountAsync();
        return $"RET-{datePrefix}-{(todayReturns + 1):D4}";
    }
}
