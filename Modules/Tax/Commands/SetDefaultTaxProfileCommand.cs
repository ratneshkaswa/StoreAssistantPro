using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Tax.Commands;

public sealed record SetDefaultTaxProfileCommand(int ProfileId) : ICommandRequest<Unit>;
