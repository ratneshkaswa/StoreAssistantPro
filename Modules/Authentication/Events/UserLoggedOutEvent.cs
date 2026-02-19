using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Authentication.Events;

public sealed record UserLoggedOutEvent(UserType UserType) : IEvent;
