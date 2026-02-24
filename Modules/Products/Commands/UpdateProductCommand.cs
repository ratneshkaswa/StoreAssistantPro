using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Products.Commands;

public sealed record UpdateProductCommand(
    int ProductId,
    string Name,
    decimal SalePrice,
    decimal CostPrice,
    int Quantity,
    int? TaxProfileId,
    string? HSNCode,
    string? Barcode,
    string UOM,
    int MinStockLevel,
    bool IsActive,
    bool IsTaxInclusive,
    byte[]? RowVersion) : ICommandRequest<Unit>;
