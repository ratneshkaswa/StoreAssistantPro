using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Users.Commands;

public sealed record ChangePinCommand(
    UserType UserType,
    string NewPin,
    string? MasterPin) : ICommand;
