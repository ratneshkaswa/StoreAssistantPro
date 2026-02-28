using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Products.Commands;

public sealed record BulkDeleteProductsCommand(
    IReadOnlyList<BulkDeleteItem> Products) : ICommandRequest<BulkDeleteProductsResult>;

public readonly record struct BulkDeleteItem(int Id, string Name, byte[]? RowVersion);

public sealed record BulkDeleteProductsResult(int Deleted, int Failed, IReadOnlyList<string> FailedNames);
