using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Products.Services;

public class CategoryService(IDbContextFactory<AppDbContext> contextFactory) : ICategoryService
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
