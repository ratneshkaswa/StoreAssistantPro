using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Brands.Services;

public interface IBrandService
{
    Task<IReadOnlyList<Brand>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Brand>> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Brand>> SearchAsync(string query, CancellationToken ct = default);
    Task CreateAsync(string name, CancellationToken ct = default);
    Task UpdateAsync(int id, string name, CancellationToken ct = default);
    Task ToggleActiveAsync(int id, CancellationToken ct = default);
}
