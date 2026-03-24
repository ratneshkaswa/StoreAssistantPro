using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Vendors.Services;

/// <summary>
/// Supplier ledger (#87) and payment tracking (#90).
/// </summary>
public interface IVendorLedgerService
{
    /// <summary>Record a payment to a vendor.</summary>
    Task<VendorPayment> RecordPaymentAsync(VendorPaymentDto dto, CancellationToken ct = default);

    /// <summary>Get all payments for a vendor, newest first.</summary>
    Task<IReadOnlyList<VendorPayment>> GetPaymentsAsync(int vendorId, CancellationToken ct = default);

    /// <summary>Get the running ledger for a vendor (opening balance + purchases − payments).</summary>
    Task<VendorLedgerSummary> GetLedgerSummaryAsync(int vendorId, CancellationToken ct = default);

    /// <summary>Get ledger entries (purchases + payments) for a vendor in chronological order.</summary>
    Task<IReadOnlyList<VendorLedgerEntry>> GetLedgerEntriesAsync(int vendorId, CancellationToken ct = default);

    /// <summary>Get vendors with outstanding balances overdue beyond their payment terms (#91).</summary>
    Task<IReadOnlyList<SupplierDueAlert>> GetOverdueAlertsAsync(CancellationToken ct = default);
}

public record VendorPaymentDto(
    int VendorId,
    decimal Amount,
    string PaymentMethod,
    string? Reference,
    string? Notes,
    int UserId);

public record VendorLedgerSummary(
    decimal OpeningBalance,
    decimal TotalPurchases,
    decimal TotalPayments,
    decimal RunningBalance,
    decimal CreditLimit);

public record VendorLedgerEntry(
    DateTime Date,
    string Type,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal Balance,
    string? Reference);

public record SupplierDueAlert(
    int VendorId,
    string VendorName,
    decimal OutstandingAmount,
    string PaymentTerms,
    int OverdueDays);
