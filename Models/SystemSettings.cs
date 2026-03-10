using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Application-wide system settings. Single row in the database.
/// Covers backup configuration, printer defaults, and tax mode.
/// </summary>
public class SystemSettings
{
    public int Id { get; set; }

    /// <summary>
    /// File system path for database backups.
    /// </summary>
    [MaxLength(500)]
    public string? BackupLocation { get; set; }

    /// <summary>Whether automatic scheduled backups are enabled.</summary>
    public bool AutoBackupEnabled { get; set; }

    /// <summary>
    /// Time of day for automatic backup (24h format as "HH:mm").
    /// </summary>
    [MaxLength(5)]
    public string? BackupTime { get; set; }

    /// <summary>
    /// Restore option (e.g., "LastBackup", "SelectFile").
    /// </summary>
    [MaxLength(50)]
    public string? RestoreOption { get; set; }

    /// <summary>
    /// Default printer name for bill/receipt printing.
    /// </summary>
    [MaxLength(200)]
    public string? DefaultPrinter { get; set; }

    /// <summary>
    /// Default tax application mode: "Inclusive" (MRP includes GST) or "Exclusive" (GST added on top).
    /// </summary>
    [MaxLength(20)]
    public string DefaultTaxMode { get; set; } = "Exclusive";

    /// <summary>Bill rounding: None, NearestOne, NearestFive, NearestTen.</summary>
    [MaxLength(20)]
    public string RoundingMethod { get; set; } = "None";

    /// <summary>Whether billing allows selling with zero or negative stock.</summary>
    public bool NegativeStockAllowed { get; set; }

    /// <summary>Language for amount-to-words on invoices: English or Hindi.</summary>
    [MaxLength(20)]
    public string NumberToWordsLanguage { get; set; } = "English";

    /// <summary>
    /// Legacy flag retained for backward compatibility with older builds.
    /// First-run completion is determined from AppConfig.IsInitialized.
    /// </summary>
    public bool SetupCompleted { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
