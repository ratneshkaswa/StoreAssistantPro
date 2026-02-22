namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Structured outcome of a <see cref="ITransactionSafetyService"/>
/// operation. Callers inspect <see cref="Succeeded"/> instead of
/// catching exceptions.
/// </summary>
public sealed class TransactionResult
{
    public bool Succeeded { get; private init; }

    /// <summary>
    /// User-facing error message. <c>null</c> on success.
    /// </summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>
    /// <c>true</c> when the failure was caused by a concurrency
    /// conflict (<see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/>).
    /// Callers can show a "data was modified by another user" prompt.
    /// </summary>
    public bool IsConcurrencyConflict { get; private init; }

    /// <summary>
    /// <c>true</c> when the failure was caused by a database constraint
    /// violation (<see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/>
    /// that is not a concurrency conflict) — e.g. unique index, FK,
    /// or check constraint. Callers can decide whether to retry or
    /// show a specific validation message.
    /// </summary>
    public bool IsConstraintViolation { get; private init; }

    /// <summary>
    /// <c>true</c> when the operation was cancelled via
    /// <see cref="CancellationToken"/> rather than failing.
    /// </summary>
    public bool IsCancelled { get; private init; }

    /// <summary>
    /// The original exception, available for logging or diagnostics.
    /// <c>null</c> on success.
    /// </summary>
    public Exception? Exception { get; private init; }

    // ── Factory helpers ────────────────────────────────────────────

    public static TransactionResult Success() => new()
    {
        Succeeded = true
    };

    public static TransactionResult Failure(
        string errorMessage,
        Exception? exception = null,
        bool isConcurrencyConflict = false,
        bool isConstraintViolation = false,
        bool isCancelled = false) => new()
    {
        Succeeded = false,
        ErrorMessage = errorMessage,
        Exception = exception,
        IsConcurrencyConflict = isConcurrencyConflict,
        IsConstraintViolation = isConstraintViolation,
        IsCancelled = isCancelled
    };
}

/// <summary>
/// Structured outcome carrying a typed value on success.
/// </summary>
/// <typeparam name="TResult">Type of the value produced by the operation.</typeparam>
public sealed class TransactionResult<TResult>
{
    public bool Succeeded { get; private init; }
    public TResult? Value { get; private init; }
    public string? ErrorMessage { get; private init; }
    public bool IsConcurrencyConflict { get; private init; }
    public bool IsConstraintViolation { get; private init; }
    public bool IsCancelled { get; private init; }
    public Exception? Exception { get; private init; }

    // ── Factory helpers ────────────────────────────────────────────

    public static TransactionResult<TResult> Success(TResult value) => new()
    {
        Succeeded = true,
        Value = value
    };

    public static TransactionResult<TResult> Failure(
        string errorMessage,
        Exception? exception = null,
        bool isConcurrencyConflict = false,
        bool isConstraintViolation = false,
        bool isCancelled = false) => new()
    {
        Succeeded = false,
        ErrorMessage = errorMessage,
        Exception = exception,
        IsConcurrencyConflict = isConcurrencyConflict,
        IsConstraintViolation = isConstraintViolation,
        IsCancelled = isCancelled
    };
}
