using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Authentication.Events;

/// <summary>
/// Published by <see cref="Services.LoginService"/> when a user
/// exceeds the maximum failed PIN attempts and is locked out,
/// or when a login is rejected because an active lockout exists.
/// </summary>
public sealed record UserLockedOutEvent(
    UserType UserType,
    DateTime Timestamp,
    DateTime LockoutEnd) : IEvent;
