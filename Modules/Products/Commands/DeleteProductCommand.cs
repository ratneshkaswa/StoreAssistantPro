using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Products.Commands;

public sealed record DeleteProductCommand(int ProductId, byte[]? RowVersion) : ICommandRequest<Unit>;
