namespace StoreAssistantPro.Models.NicheVertical;

/// <summary>Alteration order (#728-738).</summary>
public sealed class AlterationOrder
{
    public int Id { get; set; }
    public int? SaleId { get; set; }
    public int? CustomerId { get; set; }
    public string AlterationType { get; set; } = string.Empty; // Hem, TakeIn, Shorten, LetOut, Custom
    public string? Measurements { get; set; }
    public string? FittingNotes { get; set; }
    public decimal Charge { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Ready, Delivered
    public int? AssignedTailorId { get; set; }
    public DateTime? PromisedDeliveryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>Rental item tracking (#739-748).</summary>
public sealed class RentalRecord
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int? CustomerId { get; set; }
    public DateTime RentalStart { get; set; }
    public DateTime RentalEnd { get; set; }
    public DateTime? ActualReturnDate { get; set; }
    public decimal RentalPrice { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal LateFeePerDay { get; set; }
    public string Status { get; set; } = "Active"; // Active, Returned, Overdue
}

/// <summary>Wholesale pricing tier (#749-758).</summary>
public sealed class WholesalePriceTier
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int MinQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
}

/// <summary>Consignment stock record (#759-768).</summary>
public sealed class ConsignmentRecord
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ConsignorName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int SoldQuantity { get; set; }
    public decimal CommissionPercent { get; set; }
    public DateTime ReceivedDate { get; set; }
    public DateTime? SettlementDate { get; set; }
    public bool IsSettled { get; set; }
}

/// <summary>Season definition (#769-778).</summary>
public sealed class Season
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "Summer 2025"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Gift card (#779-782).</summary>
public sealed class GiftCard
{
    public int Id { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public decimal OriginalValue { get; set; }
    public decimal RemainingBalance { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int? IssuedToCustomerId { get; set; }
}

/// <summary>Loyalty program rule (#783-787).</summary>
public sealed class LoyaltyRule
{
    public int Id { get; set; }
    public decimal EarningRatio { get; set; } = 1m; // ₹1 = 1 point
    public decimal RedemptionRatio { get; set; } = 0.1m; // 1 point = ₹0.10
    public string? TierName { get; set; }
    public decimal? TierExtraPercent { get; set; }
    public bool IsActive { get; set; } = true;
}
