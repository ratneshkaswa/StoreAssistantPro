using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class AppConfig
{
    public int Id { get; set; }

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

    [Required]
    public string MasterPinHash { get; set; } = string.Empty;

    // ── Indian business configuration ──

    [MaxLength(15)]
    public string? GSTNumber { get; set; }

    [MaxLength(10)]
    public string? PANNumber { get; set; }

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
}
