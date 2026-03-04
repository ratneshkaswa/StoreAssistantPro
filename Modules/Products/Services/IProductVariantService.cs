using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Products.Services;

public interface IProductVariantService
{
    Task<IReadOnlyList<ProductVariant>> GetByProductAsync(int productId, CancellationToken ct = default);
    Task<ProductVariant?> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
    Task CreateAsync(ProductVariantDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, ProductVariantDto dto, CancellationToken ct = default);
    Task ToggleActiveAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task BulkCreateAsync(int productId, IReadOnlyList<int> sizeIds, IReadOnlyList<int> colourIds, CancellationToken ct = default);
}

public record ProductVariantDto(
    int ProductId,
    int SizeId,
    int ColourId,
    string? Barcode,
    int Quantity,
    decimal AdditionalPrice);
