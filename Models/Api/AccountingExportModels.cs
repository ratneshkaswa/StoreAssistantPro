namespace StoreAssistantPro.Models.Api;

/// <summary>
/// Accounting export format and destination configuration.
/// </summary>
public sealed class AccountingExportConfig
{
    public string Format { get; set; } = "Tally"; // Tally, QuickBooks, Zoho, CSV
    public string? ExportPath { get; set; }
    public string? ApiEndpoint { get; set; }
    public string? ApiKey { get; set; }
    public bool AutoPostEnabled { get; set; }
    public int AutoPostIntervalMinutes { get; set; } = 60;
}

/// <summary>
/// Journal entry for double-entry accounting export.
/// </summary>
public sealed record JournalEntry(
    DateTime EntryDate,
    string VoucherNumber,
    string VoucherType,
    IReadOnlyList<JournalLine> Lines,
    string? Narration);

/// <summary>
/// A single debit or credit line within a journal entry.
/// </summary>
public sealed record JournalLine(
    string LedgerName,
    decimal DebitAmount,
    decimal CreditAmount,
    string? CostCentre);

/// <summary>
/// Ledger mapping between POS account and accounting software.
/// </summary>
public sealed record LedgerMapping(
    string PosAccountName,
    string ExternalLedgerName,
    string AccountGroup,
    bool IsActive = true);

/// <summary>
/// Reconciliation result between POS data and accounting records.
/// </summary>
public sealed record ReconciliationResult(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal PosSalesTotal,
    decimal AccountingSalesTotal,
    decimal Difference,
    int UnmatchedTransactions,
    IReadOnlyList<string> Discrepancies);
