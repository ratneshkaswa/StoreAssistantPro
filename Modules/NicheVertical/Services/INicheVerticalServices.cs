using StoreAssistantPro.Models.NicheVertical;

namespace StoreAssistantPro.Modules.NicheVertical.Services;

/// <summary>Alteration management service (#727-738).</summary>
public interface IAlterationService
{
    Task<AlterationOrder> CreateOrderAsync(AlterationOrder order, CancellationToken ct = default);
    Task<AlterationOrder?> GetOrderAsync(int orderId, CancellationToken ct = default);
    Task<IReadOnlyList<AlterationOrder>> GetPendingOrdersAsync(CancellationToken ct = default);
    Task UpdateStatusAsync(int orderId, string status, CancellationToken ct = default);
    Task<IReadOnlyList<AlterationOrder>> GetCustomerHistoryAsync(int customerId, CancellationToken ct = default);
    Task AssignTailorAsync(int orderId, int tailorId, CancellationToken ct = default);
}

/// <summary>Rental management service (#739-748).</summary>
public interface IRentalService
{
    Task<RentalRecord> CreateRentalAsync(RentalRecord rental, CancellationToken ct = default);
    Task<RentalRecord?> GetRentalAsync(int rentalId, CancellationToken ct = default);
    Task<IReadOnlyList<RentalRecord>> GetActiveRentalsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RentalRecord>> GetOverdueRentalsAsync(CancellationToken ct = default);
    Task<decimal> ProcessReturnAsync(int rentalId, CancellationToken ct = default);
    Task<bool> CheckAvailabilityAsync(int productId, DateTime from, DateTime to, CancellationToken ct = default);
}

/// <summary>Wholesale/B2B pricing service (#749-758).</summary>
public interface IWholesaleService
{
    Task<IReadOnlyList<WholesalePriceTier>> GetTiersAsync(int productId, CancellationToken ct = default);
    Task SaveTiersAsync(int productId, IReadOnlyList<WholesalePriceTier> tiers, CancellationToken ct = default);
    Task<decimal> GetPriceForQuantityAsync(int productId, int quantity, CancellationToken ct = default);
}

/// <summary>Consignment management service (#759-768).</summary>
public interface IConsignmentService
{
    Task<ConsignmentRecord> AddConsignmentAsync(ConsignmentRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<ConsignmentRecord>> GetUnsettledAsync(CancellationToken ct = default);
    Task RecordSaleAsync(int consignmentId, int quantitySold, CancellationToken ct = default);
    Task SettleAsync(int consignmentId, CancellationToken ct = default);
    Task<decimal> CalculateCommissionAsync(int consignmentId, CancellationToken ct = default);
}

/// <summary>Season management service (#769-778).</summary>
public interface ISeasonService
{
    Task<IReadOnlyList<Season>> GetSeasonsAsync(CancellationToken ct = default);
    Task<Season> SaveSeasonAsync(Season season, CancellationToken ct = default);
    Task<Season?> GetActiveSeasonAsync(CancellationToken ct = default);
    Task ArchiveSeasonAsync(int seasonId, CancellationToken ct = default);
}

/// <summary>Gift card service (#779-782).</summary>
public interface IGiftCardService
{
    Task<GiftCard> IssueAsync(decimal value, DateTime expiry, int? customerId = null, CancellationToken ct = default);
    Task<GiftCard?> LookupAsync(string cardNumber, CancellationToken ct = default);
    Task<decimal> RedeemAsync(string cardNumber, decimal amount, CancellationToken ct = default);
    Task<decimal> CheckBalanceAsync(string cardNumber, CancellationToken ct = default);
}

/// <summary>Loyalty program service (#783-787).</summary>
public interface ILoyaltyService
{
    Task<LoyaltyRule?> GetActiveRuleAsync(CancellationToken ct = default);
    Task<LoyaltyRule> SaveRuleAsync(LoyaltyRule rule, CancellationToken ct = default);
    Task<int> CalculatePointsAsync(decimal purchaseAmount, CancellationToken ct = default);
    Task<decimal> CalculateRedemptionValueAsync(int points, CancellationToken ct = default);
}
