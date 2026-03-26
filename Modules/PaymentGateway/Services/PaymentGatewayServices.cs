using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.PaymentGateway;

namespace StoreAssistantPro.Modules.PaymentGateway.Services;

public sealed class PaymentGatewayService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<PaymentGatewayService> logger) : IPaymentGatewayService
{
    public async Task<IReadOnlyList<GatewayConfig>> GetConfiguredGatewaysAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.GatewayConfigs.ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<GatewayConfig> SaveGatewayConfigAsync(GatewayConfig config, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (config.Id == 0) context.GatewayConfigs.Add(config); else context.GatewayConfigs.Update(config);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Gateway config saved: {Provider}", config.Provider);
        return config;
    }

    public async Task<GatewayTransaction> InitiatePaymentAsync(PaymentGatewayProvider provider, decimal amount, int? saleId = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var txn = new GatewayTransaction
        {
            Provider = provider, Amount = amount, SaleId = saleId, CreatedAt = DateTime.UtcNow,
            GatewayTransactionId = $"TXN{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            UpiDeepLink = provider is PaymentGatewayProvider.PhonePe or PaymentGatewayProvider.GooglePay
                ? $"upi://pay?pa=merchant@upi&am={amount}&tn=Sale" : null
        };
        context.GatewayTransactions.Add(txn);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Payment initiated: {Provider} ₹{Amount}", provider, amount);
        return txn;
    }

    public async Task<GatewayTransaction> CheckPaymentStatusAsync(int transactionId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.GatewayTransactions.FindAsync([transactionId], ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Transaction {transactionId} not found");
    }

    public async Task<GatewayTransaction> ProcessRefundAsync(int transactionId, decimal? amount = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var txn = await context.GatewayTransactions.FindAsync([transactionId], ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Transaction {transactionId} not found");
        txn.Status = "Refunded";
        txn.RefundId = $"RFD{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Refund processed for txn {Id}", transactionId);
        return txn;
    }

    public Task<string> GenerateUpiLinkAsync(decimal amount, string? note = null, CancellationToken ct = default)
        => Task.FromResult($"upi://pay?pa=merchant@upi&am={amount}&tn={note ?? "Payment"}");
}

public sealed class PaymentReconciliationService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<PaymentReconciliationService> logger) : IPaymentReconciliationService
{
    public async Task<IReadOnlyList<GatewayTransaction>> GetUnreconciledAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.GatewayTransactions
            .Where(t => t.Status == "Pending").OrderBy(t => t.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task ReconcileAsync(int transactionId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var txn = await context.GatewayTransactions.FindAsync([transactionId], ct).ConfigureAwait(false);
        if (txn is null) return;
        txn.Status = "Success";
        txn.CompletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Transaction {Id} reconciled", transactionId);
    }
}

public sealed class PaymentScheduleService(
    IDbContextFactory<AppDbContext> contextFactory) : IPaymentScheduleService
{
    public async Task<PaymentSchedule> CreateScheduleAsync(PaymentSchedule schedule, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.PaymentSchedules.Add(schedule);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return schedule;
    }

    public async Task<IReadOnlyList<PaymentSchedule>> GetActiveSchedulesAsync(int? customerId = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.PaymentSchedules.Where(s => !s.IsComplete);
        if (customerId.HasValue) query = query.Where(s => s.CustomerId == customerId.Value);
        return await query.OrderBy(s => s.NextDueDate).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task RecordPaymentAsync(int scheduleId, decimal amount, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var schedule = await context.PaymentSchedules.FindAsync([scheduleId], ct).ConfigureAwait(false);
        if (schedule is null) return;
        schedule.PaidAmount += amount;
        schedule.PaidInstallments++;
        if (schedule.PaidInstallments >= schedule.TotalInstallments) schedule.IsComplete = true;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PaymentSchedule>> GetOverdueSchedulesAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.PaymentSchedules
            .Where(s => !s.IsComplete && s.NextDueDate < DateTime.UtcNow)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<EmiPlan>> GetAvailablePlansAsync(CancellationToken ct = default)
    {
        IReadOnlyList<EmiPlan> plans =
        [
            new("3 Month", 3, 0, 3000),
            new("6 Month", 6, 5, 5000),
            new("12 Month", 12, 10, 10000)
        ];
        return Task.FromResult(plans);
    }
}
