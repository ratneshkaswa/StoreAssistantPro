using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Brands.Commands;

public sealed record SaveBrandCommand(string Name, bool IsActive) : ICommandRequest<Unit>;
