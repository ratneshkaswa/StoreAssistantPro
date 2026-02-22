namespace StoreAssistantPro.Core.Commands.Validation;

/// <summary>
/// Represents a single validation failure with a property path and
/// user-facing error message.
/// </summary>
/// <param name="PropertyName">
/// The name of the property (or logical field) that failed validation.
/// Use <see cref="string.Empty"/> for cross-property or command-level
/// errors.
/// </param>
/// <param name="ErrorMessage">
/// Human-readable error message suitable for display in the UI.
/// </param>
public sealed record ValidationFailure(string PropertyName, string ErrorMessage);

/// <summary>
/// Outcome of a command validation pass. Contains zero or more
/// <see cref="ValidationFailure"/> instances.
/// <para>
/// <b>Usage:</b>
/// <code>
/// var result = ValidationResult.Success();          // no errors
/// var result = ValidationResult.WithErrors([…]);    // one or more errors
///
/// if (!result.IsValid)
///     return CommandResult&lt;int&gt;.Failure(result.ErrorMessage);
/// </code>
/// </para>
/// </summary>
public sealed class ValidationResult
{
    /// <summary><c>true</c> when the command passed all validations.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>All validation failures (empty on success).</summary>
    public IReadOnlyList<ValidationFailure> Errors { get; private init; } = [];

    /// <summary>
    /// Aggregated error message joining all failures with a newline.
    /// Returns <see cref="string.Empty"/> when valid.
    /// </summary>
    public string ErrorMessage => IsValid
        ? string.Empty
        : string.Join(Environment.NewLine,
            Errors.Select(e => string.IsNullOrEmpty(e.PropertyName)
                ? e.ErrorMessage
                : $"{e.PropertyName}: {e.ErrorMessage}"));

    /// <summary>Creates a successful (empty) validation result.</summary>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Creates a failed validation result with one or more errors.
    /// </summary>
    public static ValidationResult WithErrors(IReadOnlyList<ValidationFailure> errors) =>
        new() { Errors = errors };

    /// <summary>
    /// Shorthand for a single-property validation failure.
    /// </summary>
    public static ValidationResult WithError(string propertyName, string errorMessage) =>
        new() { Errors = [new ValidationFailure(propertyName, errorMessage)] };
}
