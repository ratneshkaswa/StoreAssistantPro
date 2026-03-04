using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Categories.Services;

public interface ICategoryService
{
    // ── Category Types (top-level grouping) ──
    Task<IReadOnlyList<CategoryType>> GetAllTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CategoryType>> GetActiveTypesAsync(CancellationToken ct = default);
    Task CreateTypeAsync(string name, CancellationToken ct = default);
    Task UpdateTypeAsync(int id, string name, CancellationToken ct = default);
    Task ToggleTypeActiveAsync(int id, CancellationToken ct = default);

    // ── Categories ──
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Category>> GetByTypeAsync(int categoryTypeId, CancellationToken ct = default);
    Task CreateAsync(CategoryDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, CategoryDto dto, CancellationToken ct = default);
    Task ToggleActiveAsync(int id, CancellationToken ct = default);

    // ── Product counts ──
    Task<int> GetProductCountAsync(int categoryId, CancellationToken ct = default);
}

public record CategoryDto(string Name, int? CategoryTypeId);
