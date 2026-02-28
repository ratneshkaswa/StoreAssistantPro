namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Published after a <see cref="Services.ITransactionSafetyService"/>
/// operation completes successfully (committed or finished without error).
/// </summary>
/// <param name="OperationId">
/// Correlates with <see cref="TransactionStartedEvent.OperationId"/>.
/// </param>
/// <param name="OperationScope">
/// Human-readable scope label.
/// </param>
/// <param name="Elapsed">
/// Wall-clock duration of the operation.
/// </param>
public sealed record TransactionCommittedEvent(
    Guid OperationId,
    string OperationScope,
    TimeSpan Elapsed) : IEvent;
