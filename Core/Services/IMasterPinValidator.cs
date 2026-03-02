namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Validates the 6-digit master PIN against the stored hash.
/// Designed for use before destructive operations (delete, restore, etc.).
/// <para>
/// <b>Security rule:</b> The raw PIN is never stored or logged.
/// Validation is always async (DB round-trip to read the hash).
/// </para>
/// </summary>
public interface IMasterPinValidator
{
    /// <summary>
    /// Prompts the user for the master PIN and validates it.
    /// Returns <c>true</c> if the PIN is correct, <c>false</c> if
    /// the user cancelled or entered a wrong PIN.
    /// </summary>
    Task<bool> ValidateAsync(string promptMessage = "Enter Master PIN to continue.", CancellationToken ct = default);
}
