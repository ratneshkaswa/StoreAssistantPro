using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Authentication.Commands;

public sealed record LogoutCommand(UserType UserType) : ICommandRequest<Unit>;
