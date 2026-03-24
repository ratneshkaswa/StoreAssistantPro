using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Discount/promotion engine implementation (#180-186, #189-190).
/// </summary>
public class DiscountService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf,
    IRegionalSettingsService regional) : IDiscountService
{
    public async Task<IReadOnlyList<DiscountRule>> GetActiveRulesAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("DiscountService.GetActiveRulesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var now = regional.Now;
        return await context.DiscountRules
            .AsNoTracking()
            .Where(r => r.IsActive &&
                        (r.ValidFrom == null || r.ValidFrom <= now) &&
                        (r.ValidTo == null || r.ValidTo >= now))
            .OrderByDescending(r => r.Priority)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DiscountRule>> GetAllRulesAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("DiscountService.GetAllRulesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.DiscountRules
            .AsNoTracking()
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.CreatedDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<DiscountRule> CreateRuleAsync(CreateDiscountRuleDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Name);

        using var _ = perf.BeginScope("DiscountService.CreateRuleAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var rule = new DiscountRule
        {
            Name = dto.Name.Trim(),
            RuleType = dto.RuleType,
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            MinBillAmount = dto.MinBillAmount,
            MaxDiscountAmount = dto.MaxDiscountAmount,
            BuyQuantity = dto.BuyQuantity,
            FreeQuantity = dto.FreeQuantity,
            CategoryId = dto.CategoryId,
            BrandId = dto.BrandId,
            CustomerId = dto.CustomerId,
            ComboProductIds = dto.ComboProductIds,
            ComboPrice = dto.ComboPrice,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            AllowStacking = dto.AllowStacking,
            Priority = dto.Priority,
            Description = dto.Description?.Trim(),
            CreatedDate = regional.Now
        };

        context.DiscountRules.Add(rule);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return rule;
    }

    public async Task UpdateRuleAsync(int id, CreateDiscountRuleDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("DiscountService.UpdateRuleAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var rule = await context.DiscountRules
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Discount rule Id {id} not found.");

        rule.Name = dto.Name.Trim();
        rule.RuleType = dto.RuleType;
        rule.DiscountType = dto.DiscountType;
        rule.DiscountValue = dto.DiscountValue;
        rule.MinBillAmount = dto.MinBillAmount;
        rule.MaxDiscountAmount = dto.MaxDiscountAmount;
        rule.BuyQuantity = dto.BuyQuantity;
        rule.FreeQuantity = dto.FreeQuantity;
        rule.CategoryId = dto.CategoryId;
        rule.BrandId = dto.BrandId;
        rule.CustomerId = dto.CustomerId;
        rule.ComboProductIds = dto.ComboProductIds;
        rule.ComboPrice = dto.ComboPrice;
        rule.ValidFrom = dto.ValidFrom;
        rule.ValidTo = dto.ValidTo;
        rule.AllowStacking = dto.AllowStacking;
        rule.Priority = dto.Priority;
        rule.Description = dto.Description?.Trim();

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeactivateRuleAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("DiscountService.DeactivateRuleAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var rule = await context.DiscountRules
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Discount rule Id {id} not found.");

        rule.IsActive = false;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AppliedDiscount>> EvaluateCartAsync(
        CartEvaluationContext evalContext, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("DiscountService.EvaluateCartAsync");

        var rules = await GetActiveRulesAsync(ct);
        var results = new List<AppliedDiscount>();
        var hasNonStackable = false;

        foreach (var rule in rules.OrderByDescending(r => r.Priority))
        {
            if (hasNonStackable && !rule.AllowStacking)
                continue;

            var discount = EvaluateRule(rule, evalContext);
            if (discount is null) continue;

            results.Add(discount);
            if (!rule.AllowStacking) hasNonStackable = true;
        }

        return results;
    }

    public async Task<AppliedDiscount?> ApplyCouponCodeAsync(
        string code, decimal billTotal, int? customerId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        using var _ = perf.BeginScope("DiscountService.ApplyCouponCodeAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var now = regional.Now;
        var coupon = await context.Coupons
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == code.Trim().ToUpperInvariant() && c.IsActive, ct)
            .ConfigureAwait(false);

        if (coupon is null)
            return null;

        if (now < coupon.ValidFrom || now > coupon.ValidTo)
            return null;

        if (coupon.MaxUsageCount > 0 && coupon.UsedCount >= coupon.MaxUsageCount)
            return null;

        if (billTotal < coupon.MinBillAmount)
            return null;

        var discountAmount = coupon.DiscountType == DiscountType.Percentage
            ? Math.Round(billTotal * coupon.DiscountValue / 100, 2)
            : coupon.DiscountValue;

        if (coupon.MaxDiscountAmount.HasValue && discountAmount > coupon.MaxDiscountAmount.Value)
            discountAmount = coupon.MaxDiscountAmount.Value;

        return new AppliedDiscount(
            0, $"Coupon: {coupon.Code}", "CouponCode",
            coupon.DiscountType, discountAmount,
            coupon.Description ?? $"Coupon {coupon.Code} applied");
    }

    // ── Private evaluation logic ──

    private static AppliedDiscount? EvaluateRule(DiscountRule rule, CartEvaluationContext ctx)
    {
        return rule.RuleType switch
        {
            "BuyXGetY" => EvaluateBuyXGetY(rule, ctx),
            "ComboBundle" => EvaluateCombo(rule, ctx),
            "Seasonal" => EvaluateSeasonal(rule, ctx),
            "Category" => EvaluateCategoryDiscount(rule, ctx),
            "Brand" => EvaluateBrandDiscount(rule, ctx),
            "CustomerSpecific" => EvaluateCustomerDiscount(rule, ctx),
            "MinBillDiscount" => EvaluateMinBill(rule, ctx),
            _ => null
        };
    }

    private static AppliedDiscount? EvaluateBuyXGetY(DiscountRule rule, CartEvaluationContext ctx)
    {
        if (rule.BuyQuantity <= 0) return null;

        var totalQty = rule.CategoryId.HasValue
            ? ctx.Items.Where(i => i.CategoryId == rule.CategoryId).Sum(i => i.Quantity)
            : ctx.Items.Sum(i => i.Quantity);

        if (totalQty < rule.BuyQuantity + rule.FreeQuantity) return null;

        var sets = totalQty / (rule.BuyQuantity + rule.FreeQuantity);
        var cheapestPrice = rule.CategoryId.HasValue
            ? ctx.Items.Where(i => i.CategoryId == rule.CategoryId).Min(i => i.UnitPrice)
            : ctx.Items.Min(i => i.UnitPrice);

        var discount = sets * rule.FreeQuantity * cheapestPrice;
        return new AppliedDiscount(rule.Id, rule.Name, rule.RuleType,
            DiscountType.Amount, discount, $"Buy {rule.BuyQuantity} Get {rule.FreeQuantity} Free");
    }

    private static AppliedDiscount? EvaluateCombo(DiscountRule rule, CartEvaluationContext ctx)
    {
        if (string.IsNullOrWhiteSpace(rule.ComboProductIds) || !rule.ComboPrice.HasValue) return null;

        var comboIds = rule.ComboProductIds.Split(',')
            .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
            .Where(id => id > 0)
            .ToHashSet();

        var cartProductIds = ctx.Items.Select(i => i.ProductId).ToHashSet();
        if (!comboIds.IsSubsetOf(cartProductIds)) return null;

        var comboItemsTotal = ctx.Items
            .Where(i => comboIds.Contains(i.ProductId))
            .Sum(i => i.UnitPrice * i.Quantity);

        var discount = comboItemsTotal - rule.ComboPrice.Value;
        if (discount <= 0) return null;

        return new AppliedDiscount(rule.Id, rule.Name, rule.RuleType,
            DiscountType.Amount, discount, rule.Description ?? "Combo discount applied");
    }

    private static AppliedDiscount? EvaluateSeasonal(DiscountRule rule, CartEvaluationContext ctx)
    {
        if (ctx.BillTotal < rule.MinBillAmount) return null;
        return CalculateDiscount(rule, ctx.BillTotal);
    }

    private static AppliedDiscount? EvaluateCategoryDiscount(DiscountRule rule, CartEvaluationContext ctx)
    {
        if (!rule.CategoryId.HasValue) return null;

        var categoryTotal = ctx.Items
            .Where(i => i.CategoryId == rule.CategoryId)
            .Sum(i => i.UnitPrice * i.Quantity);

        if (categoryTotal <= 0) return null;
        if (ctx.BillTotal < rule.MinBillAmount) return null;

        return CalculateDiscount(rule, categoryTotal);
    }

    private static AppliedDiscount? EvaluateBrandDiscount(DiscountRule rule, CartEvaluationContext ctx)
    {
        if (!rule.BrandId.HasValue) return null;

        var brandTotal = ctx.Items
            .Where(i => i.BrandId == rule.BrandId)
            .Sum(i => i.UnitPrice * i.Quantity);

        if (brandTotal <= 0) return null;
        if (ctx.BillTotal < rule.MinBillAmount) return null;

        return CalculateDiscount(rule, brandTotal);
    }

    private static AppliedDiscount? EvaluateCustomerDiscount(DiscountRule rule, CartEvaluationContext ctx)
    {
        if (!rule.CustomerId.HasValue || ctx.CustomerId != rule.CustomerId) return null;
        if (ctx.BillTotal < rule.MinBillAmount) return null;

        return CalculateDiscount(rule, ctx.BillTotal);
    }

    private static AppliedDiscount? EvaluateMinBill(DiscountRule rule, CartEvaluationContext ctx)
    {
        if (rule.MinBillAmount <= 0 || ctx.BillTotal < rule.MinBillAmount) return null;
        return CalculateDiscount(rule, ctx.BillTotal);
    }

    private static AppliedDiscount? CalculateDiscount(DiscountRule rule, decimal applicableAmount)
    {
        var discount = rule.DiscountType == DiscountType.Percentage
            ? Math.Round(applicableAmount * rule.DiscountValue / 100, 2)
            : rule.DiscountValue;

        if (rule.MaxDiscountAmount.HasValue && discount > rule.MaxDiscountAmount.Value)
            discount = rule.MaxDiscountAmount.Value;

        if (discount <= 0) return null;

        return new AppliedDiscount(rule.Id, rule.Name, rule.RuleType,
            rule.DiscountType, discount, rule.Description ?? rule.Name);
    }
}
