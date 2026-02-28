using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Authentication.Events;

/// <summary>
/// Published by <see cref="Services.LoginService"/> when a PIN
/// validation fails but the account is not yet locked.
/// </summary>
public sealed record UserLoginFailedEvent(
    UserType UserType,
    DateTime Timestamp,
    int FailedAttempts,
    int RemainingAttempts) : IEvent;
