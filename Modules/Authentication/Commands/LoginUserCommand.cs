using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Authentication.Commands;

public sealed record LoginUserCommand(UserType UserType, string Pin) : ICommand;
