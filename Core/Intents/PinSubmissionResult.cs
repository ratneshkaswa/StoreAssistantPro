namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Immutable outcome of a zero-click PIN auto-submission attempt.
/// </summary>
public sealed record PinSubmissionResult
{
    /// <summary><c>true</c> when the PIN was accepted.</summary>
    public required bool Succeeded { get; init; }

    /// <summary>Error message on failure, empty on success.</summary>
    public required string ErrorMessage { get; init; }

    /// <summary>The PIN type that was submitted (<c>"UserPin"</c> or <c>"MasterPin"</c>).</summary>
    public required string PinType { get; init; }

    /// <summary>Shortcut: successful result.</summary>
    public static PinSubmissionResult Success(string pinType) => new()
    {
        Succeeded = true,
        ErrorMessage = string.Empty,
        PinType = pinType
    };

    /// <summary>Shortcut: failed result.</summary>
    public static PinSubmissionResult Failure(string pinType, string error) => new()
    {
        Succeeded = false,
        ErrorMessage = error,
        PinType = pinType
    };
}
