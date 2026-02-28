using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Brands.Services;

public interface IBrandService
{
    Task<IReadOnlyList<Brand>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Brand>> GetAllWithProductCountAsync(CancellationToken ct = default);
    Task AddAsync(Brand brand, CancellationToken ct = default);
    Task UpdateAsync(Brand brand, CancellationToken ct = default);
    Task DeleteAsync(int id, byte[]? rowVersion, CancellationToken ct = default);
    Task<bool> HasProductsAsync(int brandId, CancellationToken ct = default);
}
