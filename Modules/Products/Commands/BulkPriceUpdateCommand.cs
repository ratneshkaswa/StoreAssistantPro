using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Products.Commands;

public sealed record BulkPriceUpdateCommand(
    int? CategoryId,
    decimal Percentage) : ICommandRequest<BulkPriceUpdateResult>;

public sealed record BulkPriceUpdateResult(int Updated);
