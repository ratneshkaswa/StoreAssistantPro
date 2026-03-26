using StoreAssistantPro.Models.PaymentGateway;

namespace StoreAssistantPro.Modules.PaymentGateway.Services;

/// <summary>Payment gateway integration service (#917-924).</summary>
public interface IPaymentGatewayService
{
    Task<IReadOnlyList<GatewayConfig>> GetConfiguredGatewaysAsync(CancellationToken ct = default);
    Task<GatewayConfig> SaveGatewayConfigAsync(GatewayConfig config, CancellationToken ct = default);
    Task<GatewayTransaction> InitiatePaymentAsync(PaymentGatewayProvider provider, decimal amount, int? saleId = null, CancellationToken ct = default);
    Task<GatewayTransaction> CheckPaymentStatusAsync(int transactionId, CancellationToken ct = default);
    Task<GatewayTransaction> ProcessRefundAsync(int transactionId, decimal? amount = null, CancellationToken ct = default);
    Task<string> GenerateUpiLinkAsync(decimal amount, string? note = null, CancellationToken ct = default);
}

/// <summary>Payment reconciliation service (#924).</summary>
public interface IPaymentReconciliationService
{
    Task<IReadOnlyList<GatewayTransaction>> GetUnreconciledAsync(CancellationToken ct = default);
    Task ReconcileAsync(int transactionId, CancellationToken ct = default);
}

/// <summary>EMI and payment schedule service (#925-928).</summary>
public interface IPaymentScheduleService
{
    Task<PaymentSchedule> CreateScheduleAsync(PaymentSchedule schedule, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentSchedule>> GetActiveSchedulesAsync(int? customerId = null, CancellationToken ct = default);
    Task RecordPaymentAsync(int scheduleId, decimal amount, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentSchedule>> GetOverdueSchedulesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EmiPlan>> GetAvailablePlansAsync(CancellationToken ct = default);
}
