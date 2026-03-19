using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class AppConfig
{
    public int Id { get; set; }
    public int SingletonKey { get; set; } = 1;

    [Required, MaxLength(100)]
    public string FirmName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(50)]
    public string State { get; set; } = string.Empty;

    [MaxLength(6)]
    public string Pincode { get; set; } = string.Empty;

    [MaxLength(15)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    public bool IsInitialized { get; set; }

    /// <summary>
    /// True when the admin PIN is still the factory default ("1234").
    /// Cleared when the admin changes their PIN via User Management.
    /// </summary>
    public bool IsDefaultAdminPin { get; set; }

    [Required]
    public string MasterPinHash { get; set; } = string.Empty;

    // ── Indian business configuration ──

    [MaxLength(15)]
    public string? GSTNumber { get; set; }

    [MaxLength(10)]
    public string? PANNumber { get; set; }

    /// <summary>GST registration type: Regular, Composition, or Unregistered.</summary>
    [MaxLength(20)]
    public string GstRegistrationType { get; set; } = "Regular";

    /// <summary>2-digit GST state code, auto-derived from GSTIN or state selection.</summary>
    [MaxLength(2)]
    public string? StateCode { get; set; }

    /// <summary>Flat tax rate for composition scheme dealers (default 1% for garments).</summary>
    public decimal CompositionSchemeRate { get; set; } = 1.0m;

    [MaxLength(10)]
    public string CurrencyCode { get; set; } = "INR";

    [MaxLength(10)]
    public string CurrencySymbol { get; set; } = "\u20B9";

    public int FinancialYearStartMonth { get; set; } = 4;  // April

    public int FinancialYearEndMonth { get; set; } = 3;    // March

    [MaxLength(20)]
    public string DateFormat { get; set; } = "dd/MM/yyyy";

    [MaxLength(20)]
    public string NumberFormat { get; set; } = "Indian";

    /// <summary>Maximum allowed discount percentage (0 = unlimited). (#179)</summary>
    public decimal MaxDiscountPercent { get; set; }
}
