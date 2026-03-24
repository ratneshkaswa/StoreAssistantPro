using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Discount/promotion engine (#180-186, #189-190).
/// Manages discount rules and applies them to billing carts.
/// </summary>
public interface IDiscountService
{
    // ── Rule CRUD ──

    /// <summary>Get all active discount rules.</summary>
    Task<IReadOnlyList<DiscountRule>> GetActiveRulesAsync(CancellationToken ct = default);

    /// <summary>Get all rules (active + inactive) for management.</summary>
    Task<IReadOnlyList<DiscountRule>> GetAllRulesAsync(CancellationToken ct = default);

    /// <summary>Create a new discount rule.</summary>
    Task<DiscountRule> CreateRuleAsync(CreateDiscountRuleDto dto, CancellationToken ct = default);

    /// <summary>Update an existing rule.</summary>
    Task UpdateRuleAsync(int id, CreateDiscountRuleDto dto, CancellationToken ct = default);

    /// <summary>Deactivate a rule.</summary>
    Task DeactivateRuleAsync(int id, CancellationToken ct = default);

    // ── Rule evaluation ──

    /// <summary>Evaluate all applicable rules for a cart and return matched discounts (#180-186, #189).</summary>
    Task<IReadOnlyList<AppliedDiscount>> EvaluateCartAsync(CartEvaluationContext context, CancellationToken ct = default);

    /// <summary>Validate and apply a coupon code (#186).</summary>
    Task<AppliedDiscount?> ApplyCouponCodeAsync(string code, decimal billTotal, int? customerId, CancellationToken ct = default);
}

// ── DTOs ──

public record CreateDiscountRuleDto(
    string Name,
    string RuleType,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal MinBillAmount = 0,
    decimal? MaxDiscountAmount = null,
    int BuyQuantity = 0,
    int FreeQuantity = 0,
    int? CategoryId = null,
    int? BrandId = null,
    int? CustomerId = null,
    string? ComboProductIds = null,
    decimal? ComboPrice = null,
    DateTime? ValidFrom = null,
    DateTime? ValidTo = null,
    bool AllowStacking = false,
    int Priority = 0,
    string? Description = null);

/// <summary>Context passed to the discount engine for evaluation.</summary>
public record CartEvaluationContext(
    IReadOnlyList<CartEvalItem> Items,
    decimal BillTotal,
    int? CustomerId);

public record CartEvalItem(
    int ProductId,
    int? CategoryId,
    int? BrandId,
    int Quantity,
    decimal UnitPrice);

/// <summary>Result of a discount rule match.</summary>
public record AppliedDiscount(
    int RuleId,
    string RuleName,
    string RuleType,
    DiscountType DiscountType,
    decimal DiscountAmount,
    string Description);
