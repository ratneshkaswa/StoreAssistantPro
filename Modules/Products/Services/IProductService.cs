using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Products.Services;

public interface IProductService
{
    // ── Products ──
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetActiveAsync(CancellationToken ct = default);
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(ProductDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, ProductDto dto, CancellationToken ct = default);
    Task ToggleActiveAsync(int id, CancellationToken ct = default);
    Task AttachTaxAsync(int productId, int? taxId, CancellationToken ct = default);

    // ── Taxes (for dropdowns) ──
    Task<IReadOnlyList<TaxMaster>> GetActiveTaxesAsync(CancellationToken ct = default);

    // ── Categories (for dropdowns) ──
    Task<IReadOnlyList<Category>> GetActiveCategoriesAsync(CancellationToken ct = default);

    // ── Brands (for dropdowns) ──
    Task<IReadOnlyList<Brand>> GetActiveBrandsAsync(CancellationToken ct = default);

    // ── Vendors (for dropdowns) ──
    Task<IReadOnlyList<Vendor>> GetActiveVendorsAsync(CancellationToken ct = default);

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
    int? TaxId,
    int? CategoryId,
    int? BrandId,
    int? VendorId,
    bool SupportsColour,
    bool SupportsPattern,
    bool SupportsSize,
    bool SupportsType);
