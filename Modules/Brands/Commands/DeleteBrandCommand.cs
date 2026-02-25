using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Brands.Commands;

public sealed record DeleteBrandCommand(int BrandId, byte[]? RowVersion) : ICommandRequest<Unit>;
