using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Sales.Events;
using StoreAssistantPro.Modules.Sales.Models;
using StoreAssistantPro.Modules.Sales.Services;

namespace StoreAssistantPro.Modules.Sales.Commands;

/// <summary>
/// Completes a sale via the online or offline path depending on
/// current connectivity.
/// </summary>
public class CompleteSaleHandler(
    ISalesService salesService,
    IEventBus eventBus,
    IBillCalculationService billCalculation,
    IRegionalSettingsService regional,
    IOfflineModeService offlineMode,
    IOfflineBillingQueue offlineQueue,
    ISessionService sessionService) : BaseCommandHandler<CompleteSaleCommand>
{
    protected override async Task<CommandResult> ExecuteAsync(CompleteSaleCommand command)
    {
        var lineSubtotal = command.Items.Sum(i => i.UnitPrice * i.Quantity);
        var summary = billCalculation.Calculate(lineSubtotal, 0m, command.Discount);

        if (offlineMode.IsOffline)
            return await EnqueueOfflineAsync(command, summary);

        return await SaveOnlineAsync(command, summary);
    }

    // ── Online path ────────────────────────────────────────────────

    private async Task<CommandResult> SaveOnlineAsync(
        CompleteSaleCommand command, BillSummary summary)
    {
        var sale = new Sale
        {
            IdempotencyKey = command.IdempotencyKey,
            InvoiceNumber = GenerateInvoiceNumber(regional.Now),
            SaleDate = regional.Now,
            TotalAmount = summary.FinalAmount,
            PaymentMethod = command.PaymentMethod,
            CashierRole = sessionService.CurrentUserType.ToString(),
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

        var result = await salesService.CreateSaleAsync(sale);

        if (!result.Succeeded)
        {
            return result.IsConcurrencyConflict
                ? CommandResult.Failure("Data was modified by another user. Please try again.")
                : CommandResult.Failure(result.ErrorMessage ?? "Sale failed.");
        }

        await eventBus.PublishAsync(new SaleCompletedEvent(result.Value, sale.TotalAmount));
        return CommandResult.Success();
    }

    // ── Offline path ───────────────────────────────────────────────

    private async Task<CommandResult> EnqueueOfflineAsync(
        CompleteSaleCommand command, BillSummary summary)
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
                CashierRole = sessionService.CurrentUserType.ToString(),
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
        return CommandResult.Success();
    }

    private static string GenerateInvoiceNumber(DateTime now) =>
        $"INV-{now:yyyyMMdd}-{now:HHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
}
