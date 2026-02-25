using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Products.Commands;

public sealed record UpdateProductCommand(
    int ProductId,
    string Name,
    decimal SalePrice,
    decimal CostPrice,
    int Quantity,
    int? TaxProfileId,
    int? BrandId,
    string? HSNCode,
    string? Barcode,
    string UOM,
    int MinStockLevel,
    int MaxStockLevel,
    bool IsActive,
    bool IsTaxInclusive,
    string? Color,
    byte[]? RowVersion) : ICommandRequest<Unit>;
