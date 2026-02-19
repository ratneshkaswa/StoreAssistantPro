using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.SystemSettings.Commands;

public sealed record ChangeMasterPinCommand(
    string CurrentPin,
    string NewPin) : ICommand;
