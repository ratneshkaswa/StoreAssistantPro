using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Products.Commands;

public sealed record SaveProductCommand(
    string Name,
    decimal SalePrice,
    int Quantity,
    int? TaxProfileId) : ICommand;
