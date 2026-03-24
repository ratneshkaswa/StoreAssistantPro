using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Vendors.Services;

public class VendorLedgerService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf) : IVendorLedgerService
{
    public async Task<VendorPayment> RecordPaymentAsync(VendorPaymentDto dto, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("VendorLedgerService.RecordPaymentAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = new VendorPayment
        {
            VendorId = dto.VendorId,
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod,
            Reference = string.IsNullOrWhiteSpace(dto.Reference) ? null : dto.Reference.Trim(),
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            PaymentDate = regional.Now,
            UserId = dto.UserId
        };

        context.VendorPayments.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return entity;
    }

    public async Task<IReadOnlyList<VendorPayment>> GetPaymentsAsync(int vendorId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("VendorLedgerService.GetPaymentsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.VendorPayments
            .AsNoTracking()
            .Where(p => p.VendorId == vendorId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<VendorLedgerSummary> GetLedgerSummaryAsync(int vendorId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("VendorLedgerService.GetLedgerSummaryAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var vendor = await context.Vendors
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == vendorId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Vendor {vendorId} not found.");

        var totalPayments = await context.VendorPayments
            .Where(p => p.VendorId == vendorId)
            .SumAsync(p => (decimal?)p.Amount, ct)
            .ConfigureAwait(false) ?? 0m;

        var totalPurchases = await GetTotalPurchasesAsync(context, vendorId, ct).ConfigureAwait(false);

        var runningBalance = vendor.OpeningBalance + totalPurchases - totalPayments;

        return new VendorLedgerSummary(
            vendor.OpeningBalance,
            totalPurchases,
            totalPayments,
            runningBalance,
            vendor.CreditLimit);
    }

    public async Task<IReadOnlyList<VendorLedgerEntry>> GetLedgerEntriesAsync(int vendorId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("VendorLedgerService.GetLedgerEntriesAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var vendor = await context.Vendors
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == vendorId, ct)
            .ConfigureAwait(false);

        if (vendor is null) return [];

        var entries = new List<VendorLedgerEntry>();

        // Opening balance as first entry
        if (vendor.OpeningBalance != 0)
        {
            entries.Add(new VendorLedgerEntry(
                vendor.CreatedDate,
                "Opening",
                "Opening Balance",
                vendor.OpeningBalance > 0 ? vendor.OpeningBalance : 0,
                vendor.OpeningBalance < 0 ? Math.Abs(vendor.OpeningBalance) : 0,
                0,
                null));
        }

        // Inward entries as purchases
        var inwardEntries = await context.InwardEntries
            .AsNoTracking()
            .Where(e => e.VendorId == vendorId)
            .OrderBy(e => e.InwardDate)
            .Select(e => new { e.InwardDate, e.InwardNumber, e.TransportCharges })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var ie in inwardEntries)
        {
            if (ie.TransportCharges > 0)
            {
                entries.Add(new VendorLedgerEntry(
                    ie.InwardDate,
                    "Purchase",
                    $"Inward {ie.InwardNumber}",
                    ie.TransportCharges,
                    0,
                    0,
                    ie.InwardNumber));
            }
        }

        // Payments
        var payments = await context.VendorPayments
            .AsNoTracking()
            .Where(p => p.VendorId == vendorId)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var p in payments)
        {
            entries.Add(new VendorLedgerEntry(
                p.PaymentDate,
                "Payment",
                $"{p.PaymentMethod} payment",
                0,
                p.Amount,
                0,
                p.Reference));
        }

        // Sort chronologically and compute running balance
        entries.Sort((a, b) => a.Date.CompareTo(b.Date));

        var balance = 0m;
        for (var i = 0; i < entries.Count; i++)
        {
            balance += entries[i].Debit - entries[i].Credit;
            entries[i] = entries[i] with { Balance = balance };
        }

        return entries;
    }

    private static async Task<decimal> GetTotalPurchasesAsync(
        AppDbContext context, int vendorId, CancellationToken ct)
    {
        return await context.InwardEntries
            .Where(e => e.VendorId == vendorId)
            .SumAsync(e => (decimal?)e.TransportCharges, ct)
            .ConfigureAwait(false) ?? 0m;
    }

    // ── Supplier due alerts (#91) ──

    public async Task<IReadOnlyList<SupplierDueAlert>> GetOverdueAlertsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("VendorLedgerService.GetOverdueAlertsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var vendors = await context.Vendors
            .AsNoTracking()
            .Where(v => v.IsActive && v.PaymentTerms != null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var alerts = new List<SupplierDueAlert>();
        var today = regional.Now.Date;

        foreach (var vendor in vendors)
        {
            var creditDays = ParseCreditDays(vendor.PaymentTerms);
            if (creditDays <= 0) continue;

            var totalPurchases = await GetTotalPurchasesAsync(context, vendor.Id, ct).ConfigureAwait(false);
            var totalPayments = await context.VendorPayments
                .Where(p => p.VendorId == vendor.Id)
                .SumAsync(p => (decimal?)p.Amount, ct)
                .ConfigureAwait(false) ?? 0m;

            var outstanding = vendor.OpeningBalance + totalPurchases - totalPayments;
            if (outstanding <= 0) continue;

            // Find the latest unpaid purchase date
            var latestPurchaseDate = await context.InwardEntries
                .Where(e => e.VendorId == vendor.Id)
                .OrderByDescending(e => e.InwardDate)
                .Select(e => (DateTime?)e.InwardDate)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (latestPurchaseDate is null) continue;

            var dueDate = latestPurchaseDate.Value.AddDays(creditDays);
            if (today <= dueDate) continue;

            alerts.Add(new SupplierDueAlert(
                vendor.Id,
                vendor.Name,
                outstanding,
                vendor.PaymentTerms ?? "N/A",
                (int)(today - dueDate).TotalDays));
        }

        return alerts.OrderByDescending(a => a.OverdueDays).ToList();
    }

    private static int ParseCreditDays(string? paymentTerms)
    {
        if (string.IsNullOrWhiteSpace(paymentTerms)) return 0;
        var term = paymentTerms.Trim().ToUpperInvariant();
        if (term == "COD" || term == "ADVANCE") return 0;
        // Parse "Net 30", "Net 60", etc.
        if (term.StartsWith("NET") && int.TryParse(term.Replace("NET", "").Trim(), out var days))
            return days;
        return 0;
    }
}
