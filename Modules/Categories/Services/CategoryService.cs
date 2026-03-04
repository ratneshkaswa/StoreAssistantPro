using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Categories.Services;

public class CategoryService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf) : ICategoryService
{
    // ── Category Types ───────────────────────────────────────────────

    public async Task<IReadOnlyList<CategoryType>> GetAllTypesAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CategoryService.GetAllTypesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.CategoryTypes
            .AsNoTracking()
            .Include(t => t.Categories)
            .OrderBy(t => t.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CategoryType>> GetActiveTypesAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CategoryService.GetActiveTypesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.CategoryTypes
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task CreateTypeAsync(string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using var _ = perf.BeginScope("CategoryService.CreateTypeAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var trimmed = name.Trim();
        if (await context.CategoryTypes.AnyAsync(t => t.Name == trimmed, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Category type '{trimmed}' already exists.");

        context.CategoryTypes.Add(new CategoryType { Name = trimmed, IsActive = true, CreatedDate = DateTime.UtcNow });
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateTypeAsync(int id, string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using var _ = perf.BeginScope("CategoryService.UpdateTypeAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.CategoryTypes.FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Category type Id {id} not found.");

        var trimmed = name.Trim();
        if (await context.CategoryTypes.AnyAsync(t => t.Name == trimmed && t.Id != id, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Category type '{trimmed}' already exists.");

        entity.Name = trimmed;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ToggleTypeActiveAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CategoryService.ToggleTypeActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.CategoryTypes.FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Category type Id {id} not found.");

        entity.IsActive = !entity.IsActive;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ── Categories ───────────────────────────────────────────────────

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CategoryService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Categories
            .AsNoTracking()
            .Include(c => c.CategoryType)
            .OrderBy(c => c.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CategoryService.GetActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Categories
            .AsNoTracking()
            .Include(c => c.CategoryType)
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Category>> GetByTypeAsync(int categoryTypeId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CategoryService.GetByTypeAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Categories
            .AsNoTracking()
            .Where(c => c.CategoryTypeId == categoryTypeId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Category>> SearchAsync(string query, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        using var _ = perf.BeginScope("CategoryService.SearchAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var term = query.Trim();
        return await context.Categories
            .AsNoTracking()
            .Include(c => c.CategoryType)
            .Where(c => c.Name.Contains(term))
            .OrderBy(c => c.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task CreateAsync(CategoryDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name, nameof(dto.Name));

        using var _ = perf.BeginScope("CategoryService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var trimmed = dto.Name.Trim();
        if (await context.Categories.AnyAsync(c => c.Name == trimmed, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Category '{trimmed}' already exists.");

        context.Categories.Add(new Category
        {
            Name = trimmed,
            CategoryTypeId = dto.CategoryTypeId,
            IsActive = true
        });

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(int id, CategoryDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name, nameof(dto.Name));

        using var _ = perf.BeginScope("CategoryService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Category Id {id} not found.");

        var trimmed = dto.Name.Trim();
        if (await context.Categories.AnyAsync(c => c.Name == trimmed && c.Id != id, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Category '{trimmed}' already exists.");

        entity.Name = trimmed;
        entity.CategoryTypeId = dto.CategoryTypeId;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CategoryService.ToggleActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Category Id {id} not found.");

        entity.IsActive = !entity.IsActive;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<int> GetProductCountAsync(int categoryId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CategoryService.GetProductCountAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Products
            .CountAsync(p => p.CategoryId == categoryId, ct)
            .ConfigureAwait(false);
    }
}
