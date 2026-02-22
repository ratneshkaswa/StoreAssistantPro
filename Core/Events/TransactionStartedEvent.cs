namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Published when a <see cref="Services.ITransactionSafetyService"/>
/// operation begins execution. Carries an <see cref="OperationId"/>
/// that correlates with the subsequent
/// <see cref="TransactionCommittedEvent"/> or
/// <see cref="TransactionFailedEvent"/>.
/// </summary>
/// <param name="OperationId">
/// Unique identifier for this operation instance. Use it to
/// correlate start → commit/fail in analytics pipelines.
/// </param>
/// <param name="OperationScope">
/// Human-readable scope label (e.g. "Transaction", "Service-layer operation").
/// </param>
public sealed record TransactionStartedEvent(
    Guid OperationId,
    string OperationScope) : IEvent;
