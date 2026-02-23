using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Tax.Commands;

public sealed record SaveTaxProfileCommand(
    int? ExistingId,
    string ProfileName,
    decimal TaxRate,
    bool IsActive) : ICommandRequest<Unit>;
