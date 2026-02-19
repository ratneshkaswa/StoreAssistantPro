using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Products.Services;

public class ProductService(IDbContextFactory<AppDbContext> contextFactory) : IProductService
{
    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Products
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task AddAsync(Product product)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        context.Products.Add(product);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            context.Products.Update(product);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                "This product was modified by another user. Please reload and try again.");
        }
    }

    public async Task DeleteAsync(int id, byte[]? rowVersion)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var product = new Product { Id = id, RowVersion = rowVersion };
        context.Products.Attach(product);

        try
        {
            context.Products.Remove(product);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                "This product was modified by another user. Please reload and try again.");
        }
    }
}
