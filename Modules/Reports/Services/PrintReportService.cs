using System.Text;
using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;

namespace StoreAssistantPro.Modules.Reports.Services;

public class PrintReportService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf,
    IRegionalSettingsService regional) : IPrintReportService
{
    private const int Width = 80;

    // ── Daily sales report (#447) ────────────────────────────────

    public async Task<string> GenerateDailySalesReportAsync(DateTime date, CancellationToken ct = default)
    {
        using var scope = perf.BeginScope("PrintReportService.GenerateDailySalesReportAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var sales = await context.Sales
            .AsNoTracking()
            .Include(s => s.Items)
            .Where(s => s.SaleDate >= dayStart && s.SaleDate < dayEnd)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var returns = await context.SaleReturns
            .AsNoTracking()
            .Where(r => r.ReturnDate >= dayStart && r.ReturnDate < dayEnd)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var firmName = config?.FirmName ?? "Store";

        var sb = new StringBuilder();

        sb.AppendLine(Center("DAILY SALES REPORT"));
        sb.AppendLine(Divider('='));
        sb.AppendLine(firmName);
        sb.AppendLine($"Date: {regional.FormatDate(date)}");
        sb.AppendLine(Divider('-'));

        sb.AppendLine($"{"Invoice",-20} {"Time",-10} {"Items",6} {"Amount",12} {"Discount",12} {"Total",12}");
        sb.AppendLine(Divider('-'));

        foreach (var sale in sales.OrderBy(s => s.SaleDate))
        {
            sb.AppendLine($"{Truncate(sale.InvoiceNumber, 20),-20} {regional.FormatTime(sale.SaleDate),-10} {sale.Items.Count,6} {sale.Items.Sum(i => i.Subtotal),12:N2} {sale.DiscountAmount,12:N2} {regional.FormatCurrency(sale.TotalAmount),12}");
        }
        sb.AppendLine(Divider('-'));

        var totalSales = sales.Sum(s => s.TotalAmount);
        var totalDiscount = sales.Sum(s => s.DiscountAmount);
        var totalReturns = returns.Sum(r => r.RefundAmount);

        sb.AppendLine(TotalLine("Total Sales", totalSales));
        sb.AppendLine(TotalLine("Total Discount", totalDiscount));
        sb.AppendLine(TotalLine("Total Returns", totalReturns));
        sb.AppendLine(Divider('='));
        sb.AppendLine(TotalLine("Net Sales", totalSales - totalReturns));
        sb.AppendLine(Divider('='));

        sb.AppendLine();
        sb.AppendLine($"Sale Count: {sales.Count}    Return Count: {returns.Count}");
        sb.AppendLine();
        sb.AppendLine(Center("Computer generated report."));

        return sb.ToString();
    }

    // ── Monthly sales report (#448) ──────────────────────────────

    public async Task<string> GenerateMonthlySalesReportAsync(int year, int month, CancellationToken ct = default)
    {
        using var scope = perf.BeginScope("PrintReportService.GenerateMonthlySalesReportAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var sales = await context.Sales
            .AsNoTracking()
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Where(s => s.SaleDate >= monthStart && s.SaleDate < monthEnd)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var firmName = config?.FirmName ?? "Store";

        var sb = new StringBuilder();

        sb.AppendLine(Center("MONTHLY SALES REPORT"));
        sb.AppendLine(Divider('='));
        sb.AppendLine(firmName);
        sb.AppendLine($"Period: {monthStart:MMMM yyyy}");
        sb.AppendLine(Divider('-'));

        // Daily summary
        sb.AppendLine($"{"Date",-15} {"Sales",8} {"Revenue",14} {"Tax",12} {"Discount",12} {"Net",14}");
        sb.AppendLine(Divider('-'));

        var dailyGroups = sales
            .GroupBy(s => s.SaleDate.Date)
            .OrderBy(g => g.Key);

        foreach (var day in dailyGroups)
        {
            var revenue = day.Sum(s => s.TotalAmount);
            var tax = day.Sum(s => s.Items.Sum(i => i.TaxAmount));
            var discount = day.Sum(s => s.DiscountAmount);
            var net = revenue;
            sb.AppendLine($"{regional.FormatDate(day.Key),-15} {day.Count(),8} {revenue,14:N2} {tax,12:N2} {discount,12:N2} {net,14:N2}");
        }
        sb.AppendLine(Divider('-'));

        var totalRevenue = sales.Sum(s => s.TotalAmount);
        var totalTax = sales.Sum(s => s.Items.Sum(i => i.TaxAmount));
        var totalDiscount = sales.Sum(s => s.DiscountAmount);
        var cogs = sales.Sum(s => s.Items.Sum(i => (i.Product?.CostPrice ?? 0) * i.Quantity));

        sb.AppendLine(TotalLine("Total Revenue", totalRevenue));
        sb.AppendLine(TotalLine("Total Tax", totalTax));
        sb.AppendLine(TotalLine("Total Discount", totalDiscount));
        sb.AppendLine(TotalLine("Cost of Goods Sold", cogs));
        sb.AppendLine(Divider('='));
        sb.AppendLine(TotalLine("Gross Profit", totalRevenue - cogs));
        sb.AppendLine(Divider('='));

        sb.AppendLine();
        sb.AppendLine($"Total Invoices: {sales.Count}    Total Items Sold: {sales.Sum(s => s.Items.Sum(i => i.Quantity))}");
        sb.AppendLine();
        sb.AppendLine(Center("Computer generated report."));

        return sb.ToString();
    }

    // ── Stock report (#449) ──────────────────────────────────────

    public async Task<string> GenerateStockReportAsync(CancellationToken ct = default)
    {
        using var scope = perf.BeginScope("PrintReportService.GenerateStockReportAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var products = await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Category!.Name)
            .ThenBy(p => p.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var firmName = config?.FirmName ?? "Store";

        var sb = new StringBuilder();

        sb.AppendLine(Center("STOCK REPORT"));
        sb.AppendLine(Divider('='));
        sb.AppendLine(firmName);
        sb.AppendLine($"Date: {regional.FormatDate(regional.Now)}");
        sb.AppendLine(Divider('-'));

        sb.AppendLine($"{"#",-4} {"Product",-22} {"Category",-14} {"Brand",-12} {"Stock",6} {"Cost",10} {"Sale",10}");
        sb.AppendLine(Divider('-'));

        var sn = 1;
        decimal totalStockValue = 0;

        foreach (var p in products)
        {
            var stockValue = p.CostPrice * p.Quantity;
            totalStockValue += stockValue;
            sb.AppendLine($"{sn++,-4} {Truncate(p.Name, 22),-22} {Truncate(p.Category?.Name ?? "-", 14),-14} {Truncate(p.Brand?.Name ?? "-", 12),-12} {p.Quantity,6} {p.CostPrice,10:N2} {p.SalePrice,10:N2}");
        }
        sb.AppendLine(Divider('-'));

        sb.AppendLine(TotalLine("Total Products", products.Count));
        sb.AppendLine(TotalLine("Total Stock Units", products.Sum(p => p.Quantity)));
        sb.AppendLine(TotalLine("Total Stock Value", totalStockValue));
        sb.AppendLine(Divider('='));

        var lowStock = products.Count(p => p.Quantity <= p.MinStockLevel && p.MinStockLevel > 0 && p.Quantity > 0);
        var outOfStock = products.Count(p => p.Quantity <= 0);
        sb.AppendLine($"Low Stock: {lowStock}    Out of Stock: {outOfStock}");
        sb.AppendLine();
        sb.AppendLine(Center("Computer generated report."));

        return sb.ToString();
    }

    // ── Customer statement (#450) ────────────────────────────────

    public async Task<string> GenerateCustomerStatementAsync(
        int customerId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var scope = perf.BeginScope("PrintReportService.GenerateCustomerStatementAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var customer = await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Customer Id {customerId} not found.");

        var sales = await context.Sales
            .AsNoTracking()
            .Where(s => s.CustomerId == customerId && s.SaleDate >= from && s.SaleDate <= to)
            .OrderBy(s => s.SaleDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var firmName = config?.FirmName ?? "Store";

        var sb = new StringBuilder();

        sb.AppendLine(Center("CUSTOMER STATEMENT"));
        sb.AppendLine(Divider('='));
        sb.AppendLine(firmName);
        sb.AppendLine(Divider('-'));

        sb.AppendLine($"Customer: {customer.Name}");
        if (!string.IsNullOrWhiteSpace(customer.Phone))
            sb.AppendLine($"Phone: {customer.Phone}");
        if (!string.IsNullOrWhiteSpace(customer.GSTIN))
            sb.AppendLine($"GSTIN: {customer.GSTIN}");
        sb.AppendLine($"Period: {regional.FormatDate(from)} to {regional.FormatDate(to)}");
        sb.AppendLine(Divider('-'));

        sb.AppendLine($"{"Date",-15} {"Invoice",-20} {"Payment",-12} {"Amount",14} {"Running",14}");
        sb.AppendLine(Divider('-'));

        decimal running = 0;
        foreach (var sale in sales)
        {
            running += sale.TotalAmount;
            sb.AppendLine($"{regional.FormatDate(sale.SaleDate),-15} {Truncate(sale.InvoiceNumber, 20),-20} {Truncate(sale.PaymentMethod, 12),-12} {regional.FormatCurrency(sale.TotalAmount),14} {regional.FormatCurrency(running),14}");
        }
        sb.AppendLine(Divider('-'));

        sb.AppendLine(TotalLine("Total Purchases", sales.Sum(s => s.TotalAmount)));
        sb.AppendLine(TotalLine("Transaction Count", sales.Count));
        sb.AppendLine(TotalLine("Lifetime Value", customer.TotalPurchaseAmount));
        sb.AppendLine(Divider('='));

        sb.AppendLine();
        sb.AppendLine(Center("Computer generated statement."));

        return sb.ToString();
    }

    // ── Purchase order print (#451) ──────────────────────────────

    public async Task<string> GeneratePurchaseOrderPrintAsync(
        int purchaseOrderId, CancellationToken ct = default)
    {
        using var scope = perf.BeginScope("PrintReportService.GeneratePurchaseOrderPrintAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var po = await context.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == purchaseOrderId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"PurchaseOrder Id {purchaseOrderId} not found.");

        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var firmName = config?.FirmName ?? "Store";
        var firmAddress = config?.Address ?? "";
        var firmPhone = config?.Phone ?? "";

        var sb = new StringBuilder();

        sb.AppendLine(Center("PURCHASE ORDER"));
        sb.AppendLine(Divider('='));
        sb.AppendLine(firmName);
        if (!string.IsNullOrWhiteSpace(firmAddress))
            sb.AppendLine(firmAddress);
        if (!string.IsNullOrWhiteSpace(firmPhone))
            sb.AppendLine($"Phone: {firmPhone}");
        sb.AppendLine(Divider('-'));

        sb.AppendLine($"PO Number: {po.OrderNumber}");
        sb.AppendLine($"Date: {regional.FormatDate(po.OrderDate)}");
        if (po.ExpectedDate.HasValue)
            sb.AppendLine($"Expected: {regional.FormatDate(po.ExpectedDate.Value)}");
        sb.AppendLine($"Status: {po.Status}");
        sb.AppendLine(Divider('-'));

        if (po.Supplier is not null)
        {
            sb.AppendLine($"Supplier: {po.Supplier.Name}");
            if (!string.IsNullOrWhiteSpace(po.Supplier.Phone))
                sb.AppendLine($"Phone: {po.Supplier.Phone}");
        }
        sb.AppendLine(Divider('-'));

        sb.AppendLine($"{"#",-4} {"Product",-30} {"Qty",8} {"Unit Cost",12} {"Total",12}");
        sb.AppendLine(Divider('-'));

        var sn = 1;
        foreach (var item in po.Items)
        {
            var name = item.Product?.Name ?? $"#{item.ProductId}";
            var total = item.Quantity * item.UnitCost;
            sb.AppendLine($"{sn++,-4} {Truncate(name, 30),-30} {item.Quantity,8} {item.UnitCost,12:N2} {total,12:N2}");
        }
        sb.AppendLine(Divider('-'));

        sb.AppendLine(TotalLine("Total Items", po.Items.Sum(i => i.Quantity)));
        sb.AppendLine(TotalLine("Total Amount", po.TotalAmount));
        sb.AppendLine(Divider('='));

        if (!string.IsNullOrWhiteSpace(po.Notes))
        {
            sb.AppendLine($"Notes: {po.Notes}");
            sb.AppendLine(Divider('-'));
        }

        sb.AppendLine();
        sb.AppendLine($"{"Authorized By: _______________",-40} {"Date: _______________"}");
        sb.AppendLine();
        sb.AppendLine(Center("Computer generated purchase order."));

        return sb.ToString();
    }

    // ── Formatting helpers ───────────────────────────────────────

    private static string Center(string text)
    {
        if (text.Length >= Width) return text;
        var pad = (Width - text.Length) / 2;
        return text.PadLeft(pad + text.Length);
    }

    private static string Divider(char c) => new(c, Width);

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..(max - 1)] + "…";

    private string TotalLine(string label, decimal amount) =>
        $"{label,-60} {regional.FormatCurrency(amount),19}";

    private static string TotalLine(string label, int count) =>
        $"{label,-60} {count,19}";
}
