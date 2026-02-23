using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Tax.Commands;

public sealed record ToggleTaxProfileCommand(int ProfileId, bool IsActive) : ICommandRequest<Unit>;
