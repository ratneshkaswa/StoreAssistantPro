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

    // ── Variant export (#63) ──
    Task<IReadOnlyList<ProductVariant>> GetAllVariantsAsync(CancellationToken ct = default);

    // ── Bulk operations ──
    Task<int> BulkAssignCategoryAsync(IReadOnlyList<int> productIds, int categoryId, CancellationToken ct = default);
    Task<int> BulkAssignBrandAsync(IReadOnlyList<int> productIds, int brandId, CancellationToken ct = default);

    // ── Barcode lookup (#387) ──
    Task<Product?> LookupByBarcodeAsync(string barcode, CancellationToken ct = default);

    // ── Default category ──
    Task<int> GetOrCreateDefaultCategoryIdAsync(CancellationToken ct = default);

    // ── Size group templates (#57) ──
    Task<IReadOnlyList<SizeGroupTemplate>> GetSizeGroupTemplatesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetSizesByGroupAsync(string groupName, CancellationToken ct = default);

    // ── Variant import (#62) ──
    Task<int> ImportVariantsAsync(IReadOnlyList<Dictionary<string, string>> rows, CancellationToken ct = default);

    // ── Multiple suppliers per product (#92) ──
    Task<IReadOnlyList<ProductSupplier>> GetProductSuppliersAsync(int productId, CancellationToken ct = default);
    Task AddProductSupplierAsync(ProductSupplierDto dto, CancellationToken ct = default);
    Task RemoveProductSupplierAsync(int productSupplierId, CancellationToken ct = default);

    /// <summary>Get best (lowest cost) supplier for a product (#93).</summary>
    Task<ProductSupplier?> GetBestSupplierAsync(int productId, CancellationToken ct = default);

    // ── Category hierarchy (#31) ──
    Task<IReadOnlyList<CategoryTreeNode>> GetCategoryTreeAsync(CancellationToken ct = default);

    // ── Supplier product count (#95) ──
    Task<IReadOnlyList<SupplierProductCount>> GetSupplierProductCountsAsync(CancellationToken ct = default);
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
    bool SupportsType,
    decimal SalePrice = 0,
    decimal CostPrice = 0,
    string? Barcode = null,
    bool IsTaxInclusive = false);

/// <summary>Predefined size group template (#57).</summary>
public record SizeGroupTemplate(string GroupName, IReadOnlyList<string> Sizes);

/// <summary>DTO for adding a product-supplier link (#92).</summary>
public record ProductSupplierDto(
    int ProductId,
    int SupplierId,
    decimal UnitCost,
    string? SupplierSKU = null,
    bool IsPrimary = false,
    int LeadTimeDays = 0,
    int MinOrderQty = 1);

/// <summary>Category tree node for hierarchy display (#31).</summary>
public record CategoryTreeNode(
    int CategoryTypeId,
    string CategoryTypeName,
    IReadOnlyList<CategoryChild> Children);

public record CategoryChild(
    int CategoryId,
    string CategoryName,
    bool IsActive,
    int ProductCount);

/// <summary>Supplier with product count (#95).</summary>
public record SupplierProductCount(
    int VendorId,
    string VendorName,
    int ProductCount);
