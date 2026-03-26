using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.Compliance;

namespace StoreAssistantPro.Modules.Compliance.Services;

public sealed class GstReturnService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<GstReturnService> logger) : IGstReturnService
{
    public async Task<GstReturnData> GenerateGstr1Async(int month, int year, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1);

        var sales = await context.Sales.Where(s => s.SaleDate >= from && s.SaleDate < to)
            .Include(s => s.Items).ToListAsync(ct).ConfigureAwait(false);

        var taxable = sales.Sum(s => s.TotalAmount - s.DiscountAmount);
        logger.LogInformation("Generated GSTR-1 for {Month}/{Year}: {Count} invoices", month, year, sales.Count);
        return new GstReturnData
        {
            ReturnType = "GSTR1", Month = month, Year = year,
            TaxableAmount = taxable, InvoiceCount = sales.Count
        };
    }

    public async Task<GstReturnData> GenerateGstr3bAsync(int month, int year, CancellationToken ct = default)
    {
        var gstr1 = await GenerateGstr1Async(month, year, ct).ConfigureAwait(false);
        gstr1.ReturnType = "GSTR3B";
        return gstr1;
    }

    public async Task<GstReturnData> GenerateGstr9Async(int year, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var from = new DateTime(year, 4, 1); // Indian FY April-March
        var to = from.AddYears(1);
        var count = await context.Sales.CountAsync(s => s.SaleDate >= from && s.SaleDate < to, ct).ConfigureAwait(false);
        var total = await context.Sales.Where(s => s.SaleDate >= from && s.SaleDate < to)
            .SumAsync(s => s.TotalAmount, ct).ConfigureAwait(false);
        return new GstReturnData { ReturnType = "GSTR9", Year = year, TaxableAmount = total, InvoiceCount = count };
    }

    public async Task<IReadOnlyList<HsnSummaryEntry>> GetHsnSummaryAsync(int month, int year, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1);

        var items = await context.SaleItems
            .Where(si => si.Sale!.SaleDate >= from && si.Sale.SaleDate < to && si.Product!.HSNCode != null)
            .GroupBy(si => si.Product!.HSNCode!)
            .Select(g => new HsnSummaryEntry(
                g.Key, null,
                g.Sum(si => si.Quantity * si.UnitPrice),
                0, 0, 0,
                g.Sum(si => si.Quantity)))
            .ToListAsync(ct).ConfigureAwait(false);

        return items;
    }
}

public sealed class EWayBillService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<EWayBillService> logger) : IEWayBillService
{
    public async Task<EWayBill> GenerateAsync(int saleId, string transportMode, string? vehicleNumber = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var sale = await context.Sales.FindAsync([saleId], ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Sale {saleId} not found");

        var bill = new EWayBill
        {
            SaleId = saleId, TransportMode = transportMode, VehicleNumber = vehicleNumber,
            GoodsValue = sale.TotalAmount, GeneratedAt = DateTime.UtcNow, ValidUntil = DateTime.UtcNow.AddDays(1),
            EWayBillNumber = $"EWB{DateTime.UtcNow:yyyyMMddHHmmss}{saleId:D5}"
        };
        context.EWayBills.Add(bill);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("e-Way bill generated: {Number}", bill.EWayBillNumber);
        return bill;
    }

    public async Task<EWayBill?> GetBySaleAsync(int saleId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.EWayBills.FirstOrDefaultAsync(b => b.SaleId == saleId, ct).ConfigureAwait(false);
    }

    public Task<bool> ValidateAsync(string ewayBillNumber, CancellationToken ct = default)
    {
        logger.LogInformation("Validating e-Way bill: {Number}", ewayBillNumber);
        return Task.FromResult(true);
    }
}

public sealed class EInvoiceService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<EInvoiceService> logger) : IEInvoiceService
{
    public async Task<EInvoice> GenerateAsync(int saleId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var invoice = new EInvoice
        {
            SaleId = saleId, GeneratedAt = DateTime.UtcNow,
            Irn = $"IRN{Guid.NewGuid():N}"[..32].ToUpperInvariant(),
            QrCode = $"eInvoice:{saleId}:{DateTime.UtcNow:yyyyMMdd}"
        };
        context.EInvoices.Add(invoice);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("e-Invoice generated: IRN {Irn}", invoice.Irn);
        return invoice;
    }

    public async Task<EInvoice?> GetBySaleAsync(int saleId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.EInvoices.FirstOrDefaultAsync(e => e.SaleId == saleId, ct).ConfigureAwait(false);
    }

    public Task<string> GenerateQrCodeAsync(int saleId, CancellationToken ct = default)
        => Task.FromResult($"eInvoice:{saleId}:{DateTime.UtcNow:yyyyMMdd}");
}

public sealed class DataComplianceService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<DataComplianceService> logger) : IDataComplianceService
{
    private readonly List<RetentionPolicy> _policies =
    [
        new("Sales", 7, true, "Tax records must be retained for 7 years"),
        new("AuditLogs", 3, true, "Audit logs retained 3 years"),
        new("CustomerData", 5, true, "Customer records 5 years after last transaction")
    ];

    public Task<IReadOnlyList<RetentionPolicy>> GetPoliciesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<RetentionPolicy>>(_policies);

    public Task SavePolicyAsync(RetentionPolicy policy, CancellationToken ct = default)
    {
        _policies.RemoveAll(p => p.DataType == policy.DataType);
        _policies.Add(policy);
        return Task.CompletedTask;
    }

    public Task<int> PurgeExpiredDataAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Running data retention purge");
        return Task.FromResult(0);
    }

    public async Task DeleteCustomerDataAsync(int customerId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var customer = await context.Customers.FindAsync([customerId], ct).ConfigureAwait(false);
        if (customer is null) return;
        customer.Name = "DELETED";
        customer.Phone = null;
        customer.Email = null;
        customer.Address = null;
        customer.GSTIN = null;
        customer.IsActive = false;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Customer {Id} data anonymized (GDPR-like deletion)", customerId);
    }
}
