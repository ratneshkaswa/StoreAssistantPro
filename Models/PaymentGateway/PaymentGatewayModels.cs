namespace StoreAssistantPro.Models.PaymentGateway;

/// <summary>Payment gateway provider (#917-920).</summary>
public enum PaymentGatewayProvider { Razorpay, PayTM, PhonePe, GooglePay }

/// <summary>Payment gateway configuration (#917-920).</summary>
public sealed class GatewayConfig
{
    public int Id { get; set; }
    public PaymentGatewayProvider Provider { get; set; }
    public string? MerchantId { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public bool IsActive { get; set; }
    public bool IsSandbox { get; set; } = true;
}

/// <summary>Gateway payment transaction (#922-924).</summary>
public sealed class GatewayTransaction
{
    public int Id { get; set; }
    public PaymentGatewayProvider Provider { get; set; }
    public string GatewayTransactionId { get; set; } = string.Empty;
    public int? SaleId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Success, Failed, Refunded
    public string? UpiDeepLink { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? RefundId { get; set; }
}

/// <summary>EMI plan definition (#925).</summary>
public sealed record EmiPlan(
    string PlanName,
    int TenureMonths,
    decimal InterestRatePercent,
    decimal MinAmount);

/// <summary>Payment plan / installment schedule (#927).</summary>
public sealed class PaymentSchedule
{
    public int Id { get; set; }
    public int? CustomerId { get; set; }
    public int? SaleId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public DateTime NextDueDate { get; set; }
    public bool IsComplete { get; set; }
}
