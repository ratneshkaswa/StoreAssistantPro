using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Products.Services;

public interface IProductService
{
    // ── Products ──
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetActiveAsync(CancellationToken ct = default);
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
    Task CreateAsync(ProductDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, ProductDto dto, CancellationToken ct = default);
    Task ToggleActiveAsync(int id, CancellationToken ct = default);
    Task AttachTaxProfileAsync(int productId, int? taxProfileId, CancellationToken ct = default);

    // ── Colours (read-only predefined palette) ──
    Task<IReadOnlyList<Colour>> GetColoursAsync(CancellationToken ct = default);

    // ── Patterns (manual entry) ──
    Task<IReadOnlyList<ProductPattern>> GetPatternsAsync(CancellationToken ct = default);
    Task CreatePatternAsync(string name, CancellationToken ct = default);

    // ── Sizes (manual entry) ──
    Task<IReadOnlyList<ProductSize>> GetSizesAsync(CancellationToken ct = default);
    Task CreateSizeAsync(string name, int sortOrder, CancellationToken ct = default);

    // ── Variant Types (manual entry) ──
    Task<IReadOnlyList<ProductVariantType>> GetVariantTypesAsync(CancellationToken ct = default);
    Task CreateVariantTypeAsync(string name, CancellationToken ct = default);
}

public record ProductDto(
    string Name,
    ProductType ProductType,
    ProductUnit Unit,
    int? TaxProfileId,
    bool SupportsColour,
    bool SupportsPattern,
    bool SupportsSize,
    bool SupportsType);
