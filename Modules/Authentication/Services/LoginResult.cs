namespace StoreAssistantPro.Modules.Authentication.Services;

/// <summary>
/// Result of a PIN validation attempt — either success or failure
/// with a user-facing error message.
/// </summary>
public sealed record LoginResult
{
    public bool Succeeded { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static LoginResult Success() =>
        new() { Succeeded = true };

    public static LoginResult Failed(string error) =>
        new() { ErrorMessage = error };
}
