using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Products.Commands;

public sealed record SaveProductCommand(
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
    bool IsTaxInclusive) : ICommandRequest<Unit>;
