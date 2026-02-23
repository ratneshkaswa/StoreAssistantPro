using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Products.Commands;

public sealed record UpdateProductCommand(
    int ProductId,
    string Name,
    decimal SalePrice,
    int Quantity,
    int? TaxProfileId,
    byte[]? RowVersion) : ICommandRequest<Unit>;
