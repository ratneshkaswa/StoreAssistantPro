using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Products.Commands;

public sealed record AdjustStockCommand(
    int ProductId,
    int AdjustmentQty,
    string? Reason,
    byte[]? RowVersion) : ICommandRequest<Unit>;
