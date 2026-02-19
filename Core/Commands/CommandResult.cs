namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Result returned by every command handler. Carries success/failure
/// plus an optional error message so ViewModels can update UI state
/// without catching exceptions from business logic.
/// </summary>
public sealed record CommandResult
{
    public bool Succeeded { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static CommandResult Success() => new() { Succeeded = true };
    public static CommandResult Failure(string error) => new() { Succeeded = false, ErrorMessage = error };
}
