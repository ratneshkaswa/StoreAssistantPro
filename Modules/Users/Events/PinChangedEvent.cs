using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Users.Events;

public sealed record PinChangedEvent(UserType UserType) : IEvent;
