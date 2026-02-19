using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Authentication.Events;

/// <summary>
/// Published by <see cref="Services.LoginService"/> when a PIN
/// validation succeeds. Carries the user type and timestamp for
/// future audit logging.
/// </summary>
public sealed record UserLoginSuccessEvent(UserType UserType, DateTime Timestamp) : IEvent;
