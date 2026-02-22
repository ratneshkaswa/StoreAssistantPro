using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Commands;

/// <summary>
/// Saves a completed bill through the enterprise pipeline.
/// <para>
/// <b>Pipeline behaviors activated:</b>
/// <list type="bullet">
///   <item><b>Validation</b> — <see cref="SaveBillCommandValidator"/>
///         ensures items, amounts, and payment method are valid before
///         the handler runs.</item>
///   <item><b>Logging</b> — command start, outcome, and duration are
///         logged automatically.</item>
///   <item><b>Offline</b> — <see cref="IOfflineCapableCommand"/> lets
///         the handler execute while offline; the handler enqueues the
///         bill for later sync.</item>
///   <item><b>Transaction</b> — <see cref="ITransactionalCommand"/>
///         wraps the handler in a transaction (commit on success,
///         rollback on failure).</item>
///   <item><b>Performance</b> — execution time is measured via
///         <c>IPerformanceMonitor</c>.</item>
/// </list>
/// </para>
/// <para>
/// <b>Result:</b> Returns the <see cref="Sale.Id"/> of the persisted
/// sale on success, or a failure message.
/// </para>
/// </summary>
public sealed record SaveBillCommand(
    Guid IdempotencyKey,
    string PaymentMethod,
    IReadOnlyList<BillItemDto> Items,
    BillDiscount Discount) : ICommandRequest<int>, ITransactionalCommand, IOfflineCapableCommand;

/// <summary>Line-item DTO carried by <see cref="SaveBillCommand"/>.</summary>
public sealed record BillItemDto(
    int ProductId,
    int Quantity,
    decimal UnitPrice);
