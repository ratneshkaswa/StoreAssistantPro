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

    /// <summary>Custom prefix for invoice numbers, e.g. "SA-" or "INV-". (#313)</summary>
    [MaxLength(20)]
    public string InvoicePrefix { get; set; } = "INV";

    /// <summary>Configurable text printed at the bottom of every receipt. (#315)</summary>
    [MaxLength(200)]
    public string ReceiptFooterText { get; set; } = "Thank you! Visit again!";

    /// <summary>File path to the firm's logo image for receipts and invoices (#311).</summary>
    [MaxLength(500)]
    public string LogoPath { get; set; } = string.Empty;

    // ── Bank details for payment receipts (#312) ──

    [MaxLength(100)]
    public string BankName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string BankAccountNumber { get; set; } = string.Empty;

    [MaxLength(11)]
    public string BankIFSC { get; set; } = string.Empty;

    /// <summary>Additional text above store name on receipt (#316).</summary>
    [MaxLength(200)]
    public string ReceiptHeaderText { get; set; } = string.Empty;

    /// <summary>Invoice numbering reset period: Never, Monthly, Annually (#314).</summary>
    [MaxLength(10)]
    public string InvoiceResetPeriod { get; set; } = "Never";

    /// <summary>Auto-discard held bills after this many minutes (0 = disabled). (#339)</summary>
    public int HeldBillTimeoutMinutes { get; set; } = 120;

    /// <summary>Maximum held bills allowed per user (0 = unlimited). (#347)</summary>
    public int MaxHeldBillsPerUser { get; set; }

    /// <summary>Decimal places for billing/display: 0 or 2 (#319).</summary>
    public int DecimalPlaces { get; set; } = 2;

    /// <summary>Store opening time as HH:mm (#320).</summary>
    [MaxLength(5)]
    public string OpeningTime { get; set; } = "09:00";

    /// <summary>Store closing time as HH:mm (#320).</summary>
    [MaxLength(5)]
    public string ClosingTime { get; set; } = "21:00";

    /// <summary>Configurable terms and conditions text printed on quotations (#361).</summary>
    [MaxLength(2000)]
    public string QuotationTermsAndConditions { get; set; } = string.Empty;

    /// <summary>Loyalty points earned per ₹100 spent (#162).</summary>
    public int LoyaltyPointsRate { get; set; } = 1;

    /// <summary>Spend threshold for Silver tier (#163).</summary>
    public decimal SilverTierThreshold { get; set; } = 10_000m;

    /// <summary>Spend threshold for Gold tier (#163).</summary>
    public decimal GoldTierThreshold { get; set; } = 50_000m;

    /// <summary>Spend threshold for Platinum tier (#163).</summary>
    public decimal PlatinumTierThreshold { get; set; } = 200_000m;

    /// <summary>Whether stock editing is frozen for physical audit (#75).</summary>
    public bool IsStockFrozen { get; set; }

    /// <summary>Stock adjustment amount above which manager approval is required. 0 = no approval (#77).</summary>
    public int StockAdjustmentApprovalThreshold { get; set; }

    /// <summary>Monthly sales target amount for dashboard progress display (#403).</summary>
    public decimal MonthlySalesTarget { get; set; }

    /// <summary>Expense amount above which manager approval is required. 0 = no approval (#235).</summary>
    public decimal ExpenseApprovalThreshold { get; set; }

    /// <summary>Startup mode: Billing or Management (#464).</summary>
    [MaxLength(20)]
    public string StartupMode { get; set; } = "Management";

    /// <summary>Whether sound effects are enabled (#463).</summary>
    public bool SoundEffectsEnabled { get; set; } = true;

    /// <summary>UI theme: Light or Dark (#457).</summary>
    [MaxLength(10)]
    public string ThemeMode { get; set; } = "Light";

    /// <summary>Font scale percentage: 100, 125, or 150 (#423).</summary>
    public int FontScalePercent { get; set; } = 100;

    /// <summary>License key for commercial activation (#465).</summary>
    [MaxLength(100)]
    public string? LicenseKey { get; set; }

    /// <summary>Last time an update check was performed (#466).</summary>
    public DateTime? LastUpdateCheck { get; set; }

    /// <summary>Default barcode symbology: EAN13, Code128, Code39, QR (#385).</summary>
    [MaxLength(10)]
    public string BarcodeFormat { get; set; } = "EAN13";

    /// <summary>Label paper layout: 65up, Roll, Custom (#386).</summary>
    [MaxLength(20)]
    public string LabelPaperSize { get; set; } = "65up";

    /// <summary>Auto-backup interval in hours. 0 = disabled (#327).</summary>
    public int BackupIntervalHours { get; set; }

    /// <summary>Whether to AES-encrypt backup files (#328).</summary>
    public bool BackupEncryptionEnabled { get; set; }

    /// <summary>Number of days to retain backups before auto-cleanup. 0 = never (#331).</summary>
    public int BackupRetentionDays { get; set; }

    /// <summary>Serialized dashboard widget layout JSON for user customization (#408).</summary>
    [MaxLength(4000)]
    public string DashboardWidgetLayout { get; set; } = string.Empty;
}
