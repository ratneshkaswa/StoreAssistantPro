using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Products.Services;

public class CategoryTypeService(IDbContextFactory<AppDbContext> contextFactory) : ICategoryTypeService
{
    public async Task<List<CategoryType>> GetAllAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.CategoryTypes.AsNoTracking()
            .Include(ct => ct.Categories)
            .OrderBy(ct => ct.Name)
            .ToListAsync();
    }

    public async Task<CategoryType> CreateAsync(CategoryType categoryType)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        db.CategoryTypes.Add(categoryType);
        await db.SaveChangesAsync();
        return categoryType;
    }

    public async Task UpdateAsync(CategoryType categoryType)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        db.CategoryTypes.Update(categoryType);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var entity = await db.CategoryTypes.FindAsync(id);
        if (entity is not null)
        {
            db.CategoryTypes.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}
