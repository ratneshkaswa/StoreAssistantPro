using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Tax.Services;

public interface ITaxService
{
    Task<IReadOnlyList<TaxMaster>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TaxMaster>> GetActiveAsync(CancellationToken ct = default);
    Task CreateAsync(TaxDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, TaxDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public record TaxDto(string TaxName, decimal SlabPercent);
