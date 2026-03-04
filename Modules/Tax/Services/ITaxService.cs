using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Tax.Services;

public interface ITaxService
{
    Task<IReadOnlyList<TaxMaster>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TaxMaster>> GetActiveAsync(CancellationToken ct = default);
    Task<TaxMaster?> GetByIdAsync(int id, CancellationToken ct = default);
    Task CreateAsync(TaxMasterDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, TaxMasterDto dto, CancellationToken ct = default);
    Task ToggleActiveAsync(int id, CancellationToken ct = default);
}

public record TaxMasterDto(
    string TaxName,
    decimal TaxRate,
    string? HSNCode,
    TaxApplicableCategory ApplicableCategory);
