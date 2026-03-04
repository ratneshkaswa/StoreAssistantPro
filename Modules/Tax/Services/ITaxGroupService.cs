using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Tax.Services;

public interface ITaxGroupService
{
    // ── Tax Groups ──
    Task<IReadOnlyList<TaxGroup>> GetAllGroupsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TaxGroup>> GetActiveGroupsAsync(CancellationToken ct = default);
    Task<TaxGroup?> GetGroupByIdAsync(int id, CancellationToken ct = default);
    Task CreateGroupAsync(TaxGroupDto dto, CancellationToken ct = default);
    Task UpdateGroupAsync(int id, TaxGroupDto dto, CancellationToken ct = default);
    Task ToggleGroupActiveAsync(int id, CancellationToken ct = default);

    // ── Tax Slabs ──
    Task<IReadOnlyList<TaxSlab>> GetSlabsByGroupAsync(int taxGroupId, CancellationToken ct = default);
    Task CreateSlabAsync(TaxSlabDto dto, CancellationToken ct = default);
    Task UpdateSlabAsync(int id, TaxSlabDto dto, CancellationToken ct = default);
    Task DeleteSlabAsync(int id, CancellationToken ct = default);

    // ── HSN Codes ──
    Task<IReadOnlyList<HSNCode>> GetAllHSNCodesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<HSNCode>> GetActiveHSNCodesAsync(CancellationToken ct = default);
    Task CreateHSNCodeAsync(HSNCodeDto dto, CancellationToken ct = default);
    Task UpdateHSNCodeAsync(int id, HSNCodeDto dto, CancellationToken ct = default);
    Task ToggleHSNActiveAsync(int id, CancellationToken ct = default);

    // ── Product Tax Mapping ──
    Task<ProductTaxMapping?> GetMappingByProductAsync(int productId, CancellationToken ct = default);
    Task SetProductMappingAsync(ProductTaxMappingDto dto, CancellationToken ct = default);
    Task RemoveProductMappingAsync(int productId, CancellationToken ct = default);

    // ── Tax Resolution (used during billing) ──

    /// <summary>
    /// Resolves the applicable GST rate for a product at a given price on a given date.
    /// Returns the matching <see cref="TaxSlab"/> or null if no mapping exists.
    /// </summary>
    Task<TaxSlab?> ResolveSlabAsync(int productId, decimal unitPrice, DateTime date, CancellationToken ct = default);

    /// <summary>
    /// Calculates the full tax breakdown for a product sale line.
    /// Supports both inclusive and exclusive GST.
    /// </summary>
    Task<TaxResult> CalculateForProductAsync(
        int productId, decimal unitPrice, decimal quantity,
        bool isIntraState, bool isTaxInclusive, DateTime date,
        CancellationToken ct = default);
}

// ── DTOs ──

public record TaxGroupDto(string Name, string? Description);

public record TaxSlabDto(
    int TaxGroupId,
    decimal GSTPercent,
    decimal PriceFrom,
    decimal PriceTo,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo);

public record HSNCodeDto(
    string Code,
    string Description,
    HSNCategory Category);

public record ProductTaxMappingDto(
    int ProductId,
    int TaxGroupId,
    int HSNCodeId,
    bool OverrideAllowed);

/// <summary>
/// Complete tax calculation result for a product sale line.
/// </summary>
public sealed record TaxResult(
    decimal BaseAmount,
    decimal GSTPercent,
    decimal CGSTAmount,
    decimal SGSTAmount,
    decimal IGSTAmount,
    decimal TotalTax,
    decimal TotalAmount,
    string? HSNCode,
    string? TaxGroupName);
