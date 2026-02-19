using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Products.Commands;

public sealed record UpdateProductCommand(
    int ProductId,
    string Name,
    decimal Price,
    int Quantity,
    byte[]? RowVersion) : ICommand;
