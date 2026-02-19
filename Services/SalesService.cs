using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Services;

public class SalesService(IDbContextFactory<AppDbContext> contextFactory) : ISalesService
{
    public async Task<IEnumerable<Sale>> GetAllAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Sales
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .AsNoTracking()
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();
    }

    public async Task<Sale?> GetByIdAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Sales
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task CreateSaleAsync(Sale sale)
    {
        await using var strategySource = await contextFactory.CreateDbContextAsync();
        var strategy = strategySource.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            var saleItems = new List<SaleItem>();
            foreach (var item in sale.Items)
            {
                var product = await context.Products.FindAsync(item.ProductId)
                    ?? throw new InvalidOperationException($"Product {item.ProductId} not found.");

                if (product.Quantity < item.Quantity)
                    throw new InvalidOperationException(
                        $"Insufficient stock for '{product.Name}'. Available: {product.Quantity}.");

                product.Quantity -= item.Quantity;
                saleItems.Add(new SaleItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            context.Sales.Add(new Sale
            {
                SaleDate = sale.SaleDate,
                TotalAmount = sale.TotalAmount,
                PaymentMethod = sale.PaymentMethod,
                Items = saleItems
            });

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    public async Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime from, DateTime to)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Sales
            .Include(s => s.Items)
            .Where(s => s.SaleDate >= from && s.SaleDate < to)
            .AsNoTracking()
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();
    }
}
