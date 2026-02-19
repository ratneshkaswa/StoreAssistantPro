namespace StoreAssistantPro.Modules.Authentication.Services;

/// <summary>
/// Result of a PIN validation attempt. Carries success/failure,
/// remaining attempts, and lockout information so the handler
/// can produce a meaningful <see cref="Core.Commands.CommandResult"/>.
/// </summary>
public sealed record LoginResult
{
    public bool Succeeded { get; private init; }
    public bool IsLockedOut { get; private init; }
    public string? ErrorMessage { get; private init; }
    public int RemainingAttempts { get; private init; }

    public static LoginResult Success() =>
        new() { Succeeded = true };

    public static LoginResult Failed(string error, int remainingAttempts) =>
        new() { ErrorMessage = error, RemainingAttempts = remainingAttempts };

    public static LoginResult LockedOut(DateTime lockoutEnd) =>
        new() { IsLockedOut = true, ErrorMessage = $"Account locked. Try again after {lockoutEnd:hh:mm:ss tt}." };
}
