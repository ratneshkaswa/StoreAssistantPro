using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Categories.Services;

public class CategoryService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf,
    IReferenceDataCache referenceDataCache) : ICategoryService
{
    private static readonly TimeSpan ReferenceDataTtl = TimeSpan.FromMinutes(5);
    private const string ActiveCategoryTypesCacheKey = "Categories.Types.Active";
    private const string ActiveCategoriesCacheKey = "Categories.Active";

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
        return await referenceDataCache.GetOrCreateAsync<CategoryType>(
            ActiveCategoryTypesCacheKey,
            async innerCt =>
            {
                await using var context = await contextFactory.CreateDbContextAsync(innerCt).ConfigureAwait(false);
                return await context.CategoryTypes
                    .AsNoTracking()
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.Name)
                    .ToListAsync(innerCt)
                    .ConfigureAwait(false);
            },
            ReferenceDataTtl,
            ct).ConfigureAwait(false);
    }

    public async Task CreateTypeAsync(string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using var _ = perf.BeginScope("CategoryService.CreateTypeAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var trimmed = name.Trim();
        if (await context.CategoryTypes.AnyAsync(t => t.Name == trimmed, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Category type '{trimmed}' already exists.");

        context.CategoryTypes.Add(new CategoryType { Name = trimmed, IsActive = true, CreatedDate = regional.Now });
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        referenceDataCache.Invalidate(ActiveCategoryTypesCacheKey);
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
        referenceDataCache.Invalidate(ActiveCategoryTypesCacheKey);
    }

    public async Task ToggleTypeActiveAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CategoryService.ToggleTypeActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.CategoryTypes.FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Category type Id {id} not found.");

        entity.IsActive = !entity.IsActive;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        referenceDataCache.Invalidate(ActiveCategoryTypesCacheKey);
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
        return await referenceDataCache.GetOrCreateAsync<Category>(
            ActiveCategoriesCacheKey,
            async innerCt =>
            {
                await using var context = await contextFactory.CreateDbContextAsync(innerCt).ConfigureAwait(false);
                return await context.Categories
                    .AsNoTracking()
                    .Include(c => c.CategoryType)
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync(innerCt)
                    .ConfigureAwait(false);
            },
            ReferenceDataTtl,
            ct).ConfigureAwait(false);
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
        referenceDataCache.Invalidate(ActiveCategoriesCacheKey);
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
        referenceDataCache.Invalidate(ActiveCategoriesCacheKey);
    }

    public async Task ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CategoryService.ToggleActiveAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Category Id {id} not found.");

        entity.IsActive = !entity.IsActive;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        referenceDataCache.Invalidate(ActiveCategoriesCacheKey);
    }

    public async Task<PagedResult<Category>> GetPagedAsync(PagedQuery query, string? search = null, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CategoryService.GetPagedAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var q = context.Categories
            .AsNoTracking()
            .Include(c => c.CategoryType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.Name.Contains(search.Trim()));

        var totalCount = await q.CountAsync(ct).ConfigureAwait(false);

        var items = await q
            .OrderBy(c => c.Name)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<Category>(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<int> GetProductCountAsync(int categoryId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CategoryService.GetProductCountAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Products
            .CountAsync(p => p.CategoryId == categoryId, ct)
            .ConfigureAwait(false);
    }

    public async Task<int> ImportBulkAsync(IReadOnlyList<string> names, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("CategoryService.ImportBulkAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var existing = await context.Categories
            .Select(c => c.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        int imported = 0;

        foreach (var name in names)
        {
            var trimmed = name.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || !existingSet.Add(trimmed))
                continue;

            context.Categories.Add(new Category { Name = trimmed, IsActive = true });
            imported++;
        }

        if (imported > 0)
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

        return imported;
    }
}
