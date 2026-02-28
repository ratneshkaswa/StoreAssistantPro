using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Brands.Commands;

public sealed record UpdateBrandCommand(
    int BrandId,
    string Name,
    bool IsActive,
    byte[]? RowVersion) : ICommandRequest<Unit>;
