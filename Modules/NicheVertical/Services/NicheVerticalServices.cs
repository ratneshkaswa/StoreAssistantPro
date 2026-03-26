using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.NicheVertical;

namespace StoreAssistantPro.Modules.NicheVertical.Services;

public sealed class AlterationService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<AlterationService> logger) : IAlterationService
{
    public async Task<AlterationOrder> CreateOrderAsync(AlterationOrder order, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        order.CreatedAt = DateTime.UtcNow;
        context.AlterationOrders.Add(order);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Alteration order created: {Type} for customer {Id}", order.AlterationType, order.CustomerId);
        return order;
    }

    public async Task<AlterationOrder?> GetOrderAsync(int orderId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.AlterationOrders.FindAsync([orderId], ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AlterationOrder>> GetPendingOrdersAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.AlterationOrders
            .Where(o => o.Status != "Delivered")
            .OrderBy(o => o.PromisedDeliveryDate).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateStatusAsync(int orderId, string status, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var order = await context.AlterationOrders.FindAsync([orderId], ct).ConfigureAwait(false);
        if (order is null) return;
        order.Status = status;
        if (status == "Delivered") order.CompletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AlterationOrder>> GetCustomerHistoryAsync(int customerId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.AlterationOrders
            .Where(o => o.CustomerId == customerId).OrderByDescending(o => o.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task AssignTailorAsync(int orderId, int tailorId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var order = await context.AlterationOrders.FindAsync([orderId], ct).ConfigureAwait(false);
        if (order is null) return;
        order.AssignedTailorId = tailorId;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

public sealed class RentalService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<RentalService> logger) : IRentalService
{
    public async Task<RentalRecord> CreateRentalAsync(RentalRecord rental, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.RentalRecords.Add(rental);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Rental created: product {Id}, {Start} to {End}", rental.ProductId, rental.RentalStart, rental.RentalEnd);
        return rental;
    }

    public async Task<RentalRecord?> GetRentalAsync(int rentalId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.RentalRecords.FindAsync([rentalId], ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RentalRecord>> GetActiveRentalsAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.RentalRecords.Where(r => r.Status == "Active").OrderBy(r => r.RentalEnd).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RentalRecord>> GetOverdueRentalsAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.RentalRecords
            .Where(r => r.Status == "Active" && r.RentalEnd < DateTime.UtcNow).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<decimal> ProcessReturnAsync(int rentalId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rental = await context.RentalRecords.FindAsync([rentalId], ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Rental {rentalId} not found");
        rental.Status = "Returned";
        rental.ActualReturnDate = DateTime.UtcNow;
        var lateDays = Math.Max(0, (rental.ActualReturnDate.Value - rental.RentalEnd).Days);
        var lateFee = lateDays * rental.LateFeePerDay;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return lateFee;
    }

    public async Task<bool> CheckAvailabilityAsync(int productId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return !await context.RentalRecords.AnyAsync(r => r.ProductId == productId && r.Status == "Active"
            && r.RentalStart < to && r.RentalEnd > from, ct).ConfigureAwait(false);
    }
}

public sealed class WholesaleService(
    IDbContextFactory<AppDbContext> contextFactory) : IWholesaleService
{
    public async Task<IReadOnlyList<WholesalePriceTier>> GetTiersAsync(int productId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.WholesalePriceTiers.Where(t => t.ProductId == productId)
            .OrderBy(t => t.MinQuantity).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task SaveTiersAsync(int productId, IReadOnlyList<WholesalePriceTier> tiers, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await context.WholesalePriceTiers.Where(t => t.ProductId == productId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        foreach (var tier in tiers) { tier.ProductId = productId; context.WholesalePriceTiers.Add(tier); }
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<decimal> GetPriceForQuantityAsync(int productId, int quantity, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var tier = await context.WholesalePriceTiers
            .Where(t => t.ProductId == productId && t.MinQuantity <= quantity)
            .OrderByDescending(t => t.MinQuantity).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (tier is not null) return tier.UnitPrice;
        var product = await context.Products.FindAsync([productId], ct).ConfigureAwait(false);
        return product?.SalePrice ?? 0;
    }
}

public sealed class ConsignmentService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<ConsignmentService> logger) : IConsignmentService
{
    public async Task<ConsignmentRecord> AddConsignmentAsync(ConsignmentRecord record, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        record.ReceivedDate = DateTime.UtcNow;
        context.ConsignmentRecords.Add(record);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Consignment added: {Consignor}, {Qty} units", record.ConsignorName, record.Quantity);
        return record;
    }

    public async Task<IReadOnlyList<ConsignmentRecord>> GetUnsettledAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.ConsignmentRecords.Where(c => !c.IsSettled).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task RecordSaleAsync(int consignmentId, int quantitySold, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var record = await context.ConsignmentRecords.FindAsync([consignmentId], ct).ConfigureAwait(false);
        if (record is null) return;
        record.SoldQuantity += quantitySold;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SettleAsync(int consignmentId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var record = await context.ConsignmentRecords.FindAsync([consignmentId], ct).ConfigureAwait(false);
        if (record is null) return;
        record.IsSettled = true;
        record.SettlementDate = DateTime.UtcNow;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public Task<decimal> CalculateCommissionAsync(int consignmentId, CancellationToken ct = default) =>
        Task.FromResult(0m);
}

public sealed class SeasonService(
    IDbContextFactory<AppDbContext> contextFactory) : ISeasonService
{
    public async Task<IReadOnlyList<Season>> GetSeasonsAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Seasons.OrderByDescending(s => s.StartDate).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<Season> SaveSeasonAsync(Season season, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (season.Id == 0) context.Seasons.Add(season); else context.Seasons.Update(season);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return season;
    }

    public async Task<Season?> GetActiveSeasonAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Seasons
            .FirstOrDefaultAsync(s => s.IsActive && s.StartDate <= DateTime.UtcNow && s.EndDate >= DateTime.UtcNow, ct)
            .ConfigureAwait(false);
    }

    public async Task ArchiveSeasonAsync(int seasonId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var season = await context.Seasons.FindAsync([seasonId], ct).ConfigureAwait(false);
        if (season is null) return;
        season.IsActive = false;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

public sealed class GiftCardService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<GiftCardService> logger) : IGiftCardService
{
    public async Task<GiftCard> IssueAsync(decimal value, DateTime expiry, int? customerId = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var card = new GiftCard
        {
            CardNumber = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
            OriginalValue = value, RemainingBalance = value, IssuedAt = DateTime.UtcNow,
            ExpiresAt = expiry, IssuedToCustomerId = customerId
        };
        context.GiftCards.Add(card);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Gift card issued: {Number}, value ₹{Value}", card.CardNumber, value);
        return card;
    }

    public async Task<GiftCard?> LookupAsync(string cardNumber, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.GiftCards.FirstOrDefaultAsync(g => g.CardNumber == cardNumber, ct).ConfigureAwait(false);
    }

    public async Task<decimal> RedeemAsync(string cardNumber, decimal amount, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var card = await context.GiftCards.FirstOrDefaultAsync(g => g.CardNumber == cardNumber && g.IsActive, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Gift card not found or inactive");
        if (card.ExpiresAt < DateTime.UtcNow) throw new InvalidOperationException("Gift card expired");
        var redeemed = Math.Min(amount, card.RemainingBalance);
        card.RemainingBalance -= redeemed;
        if (card.RemainingBalance == 0) card.IsActive = false;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return redeemed;
    }

    public async Task<decimal> CheckBalanceAsync(string cardNumber, CancellationToken ct = default)
    {
        var card = await LookupAsync(cardNumber, ct).ConfigureAwait(false);
        return card?.RemainingBalance ?? 0;
    }
}

public sealed class LoyaltyService(
    IDbContextFactory<AppDbContext> contextFactory) : ILoyaltyService
{
    public async Task<LoyaltyRule?> GetActiveRuleAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.LoyaltyRules.FirstOrDefaultAsync(r => r.IsActive, ct).ConfigureAwait(false);
    }

    public async Task<LoyaltyRule> SaveRuleAsync(LoyaltyRule rule, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (rule.Id == 0) context.LoyaltyRules.Add(rule); else context.LoyaltyRules.Update(rule);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return rule;
    }

    public async Task<int> CalculatePointsAsync(decimal purchaseAmount, CancellationToken ct = default)
    {
        var rule = await GetActiveRuleAsync(ct).ConfigureAwait(false);
        return rule is null ? 0 : (int)(purchaseAmount * rule.EarningRatio);
    }

    public async Task<decimal> CalculateRedemptionValueAsync(int points, CancellationToken ct = default)
    {
        var rule = await GetActiveRuleAsync(ct).ConfigureAwait(false);
        return rule is null ? 0 : points * rule.RedemptionRatio;
    }
}
