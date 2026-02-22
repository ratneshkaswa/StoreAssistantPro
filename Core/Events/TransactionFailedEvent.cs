namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Published when a <see cref="Services.ITransactionSafetyService"/>
/// operation fails (rolled back or caught an exception).
/// </summary>
/// <param name="OperationId">
/// Correlates with <see cref="TransactionStartedEvent.OperationId"/>.
/// </param>
/// <param name="OperationScope">
/// Human-readable scope label.
/// </param>
/// <param name="Elapsed">
/// Wall-clock duration before the failure was detected.
/// </param>
/// <param name="ErrorMessage">
/// Sanitised, user-facing error message from the
/// <see cref="Services.TransactionResult"/>.
/// </param>
/// <param name="IsConcurrencyConflict">
/// <c>true</c> when the failure was a concurrency conflict.
/// </param>
/// <param name="IsConstraintViolation">
/// <c>true</c> when the failure was a database constraint violation.
/// </param>
/// <param name="IsCancelled">
/// <c>true</c> when the operation was cancelled.
/// </param>
public sealed record TransactionFailedEvent(
    Guid OperationId,
    string OperationScope,
    TimeSpan Elapsed,
    string ErrorMessage,
    bool IsConcurrencyConflict,
    bool IsConstraintViolation,
    bool IsCancelled) : IEvent;
