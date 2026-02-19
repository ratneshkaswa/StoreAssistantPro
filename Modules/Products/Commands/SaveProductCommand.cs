using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Products.Commands;

public sealed record SaveProductCommand(
    string Name,
    decimal Price,
    int Quantity) : ICommand;
