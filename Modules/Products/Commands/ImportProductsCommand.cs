using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Products.Commands;

public sealed record ImportProductsCommand(string FilePath) : ICommandRequest<ImportProductsResult>;

public sealed record ImportProductsResult(int Imported, int Skipped, IReadOnlyList<string> Errors);
