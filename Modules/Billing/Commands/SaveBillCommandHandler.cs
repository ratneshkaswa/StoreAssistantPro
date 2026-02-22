using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Sales.Events;
using StoreAssistantPro.Modules.Sales.Models;
using StoreAssistantPro.Modules.Sales.Services;

namespace StoreAssistantPro.Modules.Billing.Commands;

/// <summary>
/// Handles <see cref="SaveBillCommand"/> — persists a bill online or
/// enqueues it for offline sync.
/// <para>
/// <b>Pipeline flow (behaviors execute in this order before the handler):</b>
/// <code>
/// SendAsync&lt;SaveBillCommand, int&gt;(command)
///   │
///   ▼  ValidationPipelineBehavior   → SaveBillCommandValidator
///   ▼  LoggingPipelineBehavior      → logs start / outcome / duration
///   ▼  OfflinePipelineBehavior      → IOfflineCapableCommand → passes through
///   ▼  TransactionPipelineBehavior  → ITransactionalCommand → wraps in tx
///   ▼  PerformancePipelineBehavior  → IPerformanceMonitor scope
///   ▼  SaveBillCommandHandler (this)
/// </code>
/// </para>
/// <para>
/// <b>Online path:</b> Builds a <see cref="Sale"/> entity and persists
/// it through <see cref="ISalesService.CreateSaleAsync"/>. Returns the
/// new <see cref="Sale.Id"/>.
/// </para>
/// <para>
/// <b>Offline path:</b> Builds an <see cref="OfflineBill"/> and
/// enqueues it in <see cref="IOfflineBillingQueue"/>. Returns a
/// sentinel ID of <c>0</c> (no DB identity available offline).
/// </para>
/// </summary>
public sealed class SaveBillCommandHandler(
    ISalesService salesService,
    IEventBus eventBus,
    IBillCalculationService billCalculation,
    IRegionalSettingsService regional,
    IOfflineModeService offlineMode,
    IOfflineBillingQueue offlineQueue)
    : ICommandRequestHandler<SaveBillCommand, int>
{
    public async Task<CommandResult<int>> HandleAsync(
        SaveBillCommand command, CancellationToken ct = default)
    {
        var lineSubtotal = command.Items.Sum(i => i.UnitPrice * i.Quantity);
        var summary = billCalculation.Calculate(lineSubtotal, 0m, command.Discount);

        if (offlineMode.IsOffline)
            return await EnqueueOfflineAsync(command, summary);

        return await SaveOnlineAsync(command, summary, ct);
    }

    // ── Online path ────────────────────────────────────────────────

    private async Task<CommandResult<int>> SaveOnlineAsync(
        SaveBillCommand command, BillSummary summary, CancellationToken ct)
    {
        var sale = new Sale
        {
            IdempotencyKey = command.IdempotencyKey,
            SaleDate = regional.Now,
            TotalAmount = summary.FinalAmount,
            PaymentMethod = command.PaymentMethod,
            DiscountType = command.Discount.Type,
            DiscountValue = command.Discount.Value,
            DiscountAmount = summary.DiscountAmount,
            DiscountReason = command.Discount.Reason,
            Items = command.Items.Select(i => new SaleItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        var result = await salesService.CreateSaleAsync(sale, ct);

        if (!result.Succeeded)
        {
            return result.IsConcurrencyConflict
                ? CommandResult<int>.Failure(
                    "Data was modified by another user. Please try again.")
                : CommandResult<int>.Failure(
                    result.ErrorMessage ?? "Failed to save bill.");
        }

        await eventBus.PublishAsync(
            new SaleCompletedEvent(result.Value, sale.TotalAmount));

        return CommandResult<int>.Success(result.Value);
    }

    // ── Offline path ───────────────────────────────────────────────

    private async Task<CommandResult<int>> EnqueueOfflineAsync(
        SaveBillCommand command, BillSummary summary)
    {
        var bill = new OfflineBill
        {
            IdempotencyKey = command.IdempotencyKey,
            CreatedTime = regional.Now,
            Status = OfflineBillStatus.PendingSync,
            Sale = new CompleteSaleSnapshot
            {
                TotalAmount = summary.FinalAmount,
                PaymentMethod = command.PaymentMethod,
                SaleDate = regional.Now,
                DiscountType = command.Discount.Type,
                DiscountValue = command.Discount.Value,
                DiscountAmount = summary.DiscountAmount,
                DiscountReason = command.Discount.Reason,
                Items = command.Items.Select(i => new SaleItemSnapshot
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            }
        };

        await offlineQueue.EnqueueAsync(bill);
        await eventBus.PublishAsync(
            new SaleQueuedOfflineEvent(command.IdempotencyKey, summary.FinalAmount));

        return CommandResult<int>.Success(0);
    }
}
