using System.Text;
using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

public class ReceiptService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf,
    IRegionalSettingsService regional) : IReceiptService
{
    private const int LineWidth = 42; // 80mm thermal ≈ 42 chars at default font

    public async Task<string> GenerateThermalReceiptAsync(int saleId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReceiptService.GenerateThermalReceiptAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var sale = await context.Sales
            .AsNoTracking()
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == saleId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Sale Id {saleId} not found.");

        // Load firm info from AppConfig
        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var firmName = config?.FirmName ?? "Store";
        var firmAddress = config?.Address ?? "";
        var firmPhone = config?.Phone ?? "";
        var firmGSTIN = config?.GSTNumber ?? "";
        var footerText = string.IsNullOrWhiteSpace(config?.ReceiptFooterText)
            ? "Thank you! Visit again!"
            : config.ReceiptFooterText;

        return GenerateThermalReceipt(sale, firmName, firmAddress, firmPhone, firmGSTIN, footerText);
    }

    public string GenerateThermalReceipt(
        Sale sale, string firmName, string firmAddress,
        string firmPhone, string firmGSTIN, string footerText)
    {
        var sb = new StringBuilder();

        // Header — firm details
        sb.AppendLine(Center(firmName));
        if (!string.IsNullOrWhiteSpace(firmAddress))
            sb.AppendLine(Center(firmAddress));
        if (!string.IsNullOrWhiteSpace(firmPhone))
            sb.AppendLine(Center($"Ph: {firmPhone}"));
        if (!string.IsNullOrWhiteSpace(firmGSTIN))
            sb.AppendLine(Center($"GSTIN: {firmGSTIN}"));
        sb.AppendLine(Divider());

        // Invoice info
        sb.AppendLine($"Invoice: {sale.InvoiceNumber}");
        sb.AppendLine($"Date: {regional.FormatDate(sale.SaleDate)} {regional.FormatTime(sale.SaleDate)}");
        if (!string.IsNullOrWhiteSpace(sale.CashierRole))
            sb.AppendLine($"Cashier: {sale.CashierRole}");
        sb.AppendLine(Divider());

        // Column headers
        sb.AppendLine(FormatLine("Item", "Qty", "Rate", "Total"));
        sb.AppendLine(Divider());

        // Line items
        foreach (var item in sale.Items)
        {
            var name = Truncate(item.Product?.Name ?? $"#{item.ProductId}", 18);
            var qty = item.Quantity.ToString();
            var rate = regional.FormatNumber(item.UnitPrice);
            var total = regional.FormatCurrency(item.Subtotal);

            sb.AppendLine(FormatLine(name, qty, rate, total));

            if (item.ItemDiscountRate > 0)
                sb.AppendLine($"  Disc: {item.ItemDiscountRate}%");
        }
        sb.AppendLine(Divider());

        // Totals
        sb.AppendLine(TotalLine("Subtotal", sale.Items.Sum(i => i.Subtotal)));

        if (sale.DiscountAmount > 0)
        {
            var discLabel = sale.DiscountType == DiscountType.Percentage
                ? $"Discount ({sale.DiscountValue}%)"
                : "Discount";
            sb.AppendLine(TotalLine(discLabel, -sale.DiscountAmount));
        }

        sb.AppendLine(Divider());
        sb.AppendLine(TotalLine("TOTAL", sale.TotalAmount));
        sb.AppendLine(Divider());

        // Payment
        sb.AppendLine($"Paid by: {sale.PaymentMethod}");
        if (!string.IsNullOrWhiteSpace(sale.PaymentReference))
            sb.AppendLine($"Ref: {sale.PaymentReference}");
        sb.AppendLine(Divider());

        // Footer
        if (!string.IsNullOrWhiteSpace(footerText))
            sb.AppendLine(Center(footerText));

        return sb.ToString();
    }

    public async Task<string> GenerateReturnReceiptAsync(int returnId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReceiptService.GenerateReturnReceiptAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var ret = await context.SaleReturns
            .AsNoTracking()
            .Include(r => r.Sale)
            .Include(r => r.SaleItem).ThenInclude(si => si!.Product)
            .FirstOrDefaultAsync(r => r.Id == returnId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"SaleReturn Id {returnId} not found.");

        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var firmName = config?.FirmName ?? "Store";
        var firmAddress = config?.Address ?? "";
        var firmPhone = config?.Phone ?? "";
        var firmGSTIN = config?.GSTNumber ?? "";

        var sb = new StringBuilder();

        // Header
        sb.AppendLine(Center(firmName));
        if (!string.IsNullOrWhiteSpace(firmAddress))
            sb.AppendLine(Center(firmAddress));
        if (!string.IsNullOrWhiteSpace(firmPhone))
            sb.AppendLine(Center($"Ph: {firmPhone}"));
        if (!string.IsNullOrWhiteSpace(firmGSTIN))
            sb.AppendLine(Center($"GSTIN: {firmGSTIN}"));
        sb.AppendLine(Divider());
        sb.AppendLine(Center("** CREDIT NOTE **"));
        sb.AppendLine(Divider());

        // Credit note info
        sb.AppendLine($"Credit Note: {ret.CreditNoteNumber}");
        sb.AppendLine($"Return Ref: {ret.ReturnNumber}");
        sb.AppendLine($"Orig Invoice: {ret.Sale?.InvoiceNumber ?? "N/A"}");
        sb.AppendLine($"Date: {regional.FormatDate(ret.ReturnDate)} {regional.FormatTime(ret.ReturnDate)}");
        sb.AppendLine(Divider());

        // Returned item
        sb.AppendLine(FormatLine("Item", "Qty", "Rate", "Refund"));
        sb.AppendLine(Divider());

        var productName = Truncate(ret.SaleItem?.Product?.Name ?? $"#{ret.SaleItem?.ProductId}", 18);
        var unitPrice = ret.SaleItem?.DiscountedUnitPrice ?? 0;
        sb.AppendLine(FormatLine(productName, ret.Quantity.ToString(), regional.FormatNumber(unitPrice), regional.FormatCurrency(ret.RefundAmount)));
        sb.AppendLine(Divider());

        // Totals
        sb.AppendLine(TotalLine("REFUND AMOUNT", ret.RefundAmount));
        sb.AppendLine(Divider());

        // Reason
        sb.AppendLine($"Reason: {ret.Reason}");
        sb.AppendLine(Divider());
        sb.AppendLine(Center("Thank you!"));

        return sb.ToString();
    }

    // ── Formatting helpers ───────────────────────────────────────

    private const int A4Width = 80; // character width for A4 text invoice

    public async Task<string> GenerateA4InvoiceAsync(int saleId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReceiptService.GenerateA4InvoiceAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var sale = await context.Sales
            .AsNoTracking()
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == saleId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Sale Id {saleId} not found.");

        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var firmName = config?.FirmName ?? "Store";
        var firmAddress = config?.Address ?? "";
        var firmPhone = config?.Phone ?? "";
        var firmGSTIN = config?.GSTNumber ?? "";
        var firmState = config?.State ?? "";

        var sb = new StringBuilder();

        // ── Header: TAX INVOICE ──
        sb.AppendLine(A4Center("TAX INVOICE"));
        sb.AppendLine(A4Divider('='));

        // ── Seller details (left) ──
        sb.AppendLine(firmName);
        if (!string.IsNullOrWhiteSpace(firmAddress))
            sb.AppendLine(firmAddress);
        if (!string.IsNullOrWhiteSpace(firmState))
            sb.AppendLine($"State: {firmState}");
        if (!string.IsNullOrWhiteSpace(firmGSTIN))
            sb.AppendLine($"GSTIN: {firmGSTIN}");
        if (!string.IsNullOrWhiteSpace(firmPhone))
            sb.AppendLine($"Phone: {firmPhone}");
        sb.AppendLine(A4Divider('-'));

        // ── Buyer details (#134) ──
        if (sale.Customer is not null)
        {
            sb.AppendLine("Bill To:");
            sb.AppendLine($"  Name: {sale.Customer.Name}");
            if (!string.IsNullOrWhiteSpace(sale.Customer.Address))
                sb.AppendLine($"  Address: {sale.Customer.Address}");
            if (!string.IsNullOrWhiteSpace(sale.Customer.Phone))
                sb.AppendLine($"  Phone: {sale.Customer.Phone}");
            if (!string.IsNullOrWhiteSpace(sale.Customer.GSTIN))
                sb.AppendLine($"  GSTIN: {sale.Customer.GSTIN}");
        }
        else
        {
            sb.AppendLine("Bill To: Walk-in Customer");
        }
        sb.AppendLine(A4Divider('-'));

        // ── Invoice metadata ──
        sb.AppendLine($"Invoice No: {sale.InvoiceNumber,-40} Date: {regional.FormatDate(sale.SaleDate)}");
        sb.AppendLine($"Payment: {sale.PaymentMethod,-40} Time: {regional.FormatTime(sale.SaleDate)}");
        if (!string.IsNullOrWhiteSpace(sale.PaymentReference))
            sb.AppendLine($"Ref: {sale.PaymentReference}");
        sb.AppendLine(A4Divider('='));

        // ── Item table header ──
        sb.AppendLine(A4ItemHeader());
        sb.AppendLine(A4Divider('-'));

        // ── Line items ──
        var sn = 1;
        foreach (var item in sale.Items)
        {
            var name = item.Product?.Name ?? $"#{item.ProductId}";
            var hsn = item.Product?.HSNCode ?? "";
            var qty = item.Quantity;
            var rate = item.UnitPrice;
            var taxable = item.Subtotal;
            var cgst = item.CgstAmount;
            var sgst = item.SgstAmount;
            var total = taxable + item.TaxAmount;

            sb.AppendLine(A4ItemLine(sn++, name, hsn, qty, rate, item.TaxRate, taxable, cgst, sgst, total));
        }
        sb.AppendLine(A4Divider('-'));

        // ── Totals ──
        var subtotal = sale.Items.Sum(i => i.Subtotal);
        var totalCgst = sale.Items.Sum(i => i.CgstAmount);
        var totalSgst = sale.Items.Sum(i => i.SgstAmount);
        var totalTax = sale.Items.Sum(i => i.TaxAmount);

        sb.AppendLine(A4TotalLine("Subtotal", subtotal));
        sb.AppendLine(A4TotalLine("CGST", totalCgst));
        sb.AppendLine(A4TotalLine("SGST", totalSgst));

        if (sale.DiscountAmount > 0)
        {
            var discLabel = sale.DiscountType == DiscountType.Percentage
                ? $"Discount ({sale.DiscountValue}%)"
                : "Discount";
            sb.AppendLine(A4TotalLine(discLabel, -sale.DiscountAmount));
        }

        sb.AppendLine(A4Divider('='));
        sb.AppendLine(A4TotalLine("GRAND TOTAL", sale.TotalAmount));
        sb.AppendLine(A4Divider('='));

        // ── Tax summary table (HSN-wise) ──
        sb.AppendLine();
        sb.AppendLine("HSN Summary:");
        sb.AppendLine($"{"HSN",-10} {"Taxable",12} {"CGST%",6} {"CGST",10} {"SGST%",6} {"SGST",10} {"Total Tax",12}");
        sb.AppendLine(A4Divider('-'));

        var hsnGroups = sale.Items
            .GroupBy(i => i.Product?.HSNCode ?? "N/A")
            .OrderBy(g => g.Key);

        foreach (var g in hsnGroups)
        {
            var first = g.First();
            sb.AppendLine($"{g.Key,-10} {g.Sum(i => i.Subtotal),12:N2} {first.CgstRate,5:N1}% {g.Sum(i => i.CgstAmount),10:N2} {first.SgstRate,5:N1}% {g.Sum(i => i.SgstAmount),10:N2} {g.Sum(i => i.TaxAmount),12:N2}");
        }
        sb.AppendLine(A4Divider('-'));

        // ── Footer ──
        sb.AppendLine();
        sb.AppendLine("Terms & Conditions:");
        sb.AppendLine("  1. Goods once sold will not be taken back.");
        sb.AppendLine("  2. Subject to local jurisdiction.");
        sb.AppendLine();
        sb.AppendLine(A4Center("This is a computer generated invoice."));

        return sb.ToString();
    }

    private static string A4Center(string text)
    {
        if (text.Length >= A4Width) return text;
        var pad = (A4Width - text.Length) / 2;
        return text.PadLeft(pad + text.Length);
    }

    private static string A4Divider(char c) => new(c, A4Width);

    private static string A4ItemHeader() =>
        $"{"#",-3} {"Item",-20} {"HSN",-8} {"Qty",4} {"Rate",8} {"GST%",5} {"Taxable",10} {"CGST",8} {"SGST",8} {"Total",10}";

    private static string A4ItemLine(int sn, string name, string hsn, int qty, decimal rate,
        decimal gstRate, decimal taxable, decimal cgst, decimal sgst, decimal total)
    {
        var truncName = name.Length > 20 ? name[..19] + "…" : name;
        var truncHsn = hsn.Length > 8 ? hsn[..7] + "…" : hsn;
        return $"{sn,-3} {truncName,-20} {truncHsn,-8} {qty,4} {rate,8:N2} {gstRate,4:N1}% {taxable,10:N2} {cgst,8:N2} {sgst,8:N2} {total,10:N2}";
    }

    private string A4TotalLine(string label, decimal amount) =>
        $"{label,-60} {regional.FormatCurrency(amount),19}";

    // ── 80mm formatting helpers ──────────────────────────────────

    private static string Center(string text)
    {
        if (text.Length >= LineWidth) return text;
        var pad = (LineWidth - text.Length) / 2;
        return text.PadLeft(pad + text.Length);
    }

    private static string Divider() => new('-', LineWidth);

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..(max - 1)] + "…";

    private static string FormatLine(string col1, string col2, string col3, string col4)
    {
        // 18 + 4 + 8 + 9 + 3 spaces = 42
        return $"{col1,-18} {col2,4} {col3,8} {col4,9}";
    }

    private string TotalLine(string label, decimal amount) =>
        $"{label,-28} {regional.FormatCurrency(amount),13}";

    // ── 58mm narrow receipt (#440) ───────────────────────────────

    private const int NarrowWidth = 32; // 58mm thermal ≈ 32 chars

    public async Task<string> Generate58mmReceiptAsync(int saleId, CancellationToken ct = default)
    {
        using var scope = perf.BeginScope("ReceiptService.Generate58mmReceiptAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var sale = await context.Sales
            .AsNoTracking()
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == saleId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Sale Id {saleId} not found.");

        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var firmName = config?.FirmName ?? "Store";
        var firmPhone = config?.Phone ?? "";
        var firmGSTIN = config?.GSTNumber ?? "";
        var footerText = string.IsNullOrWhiteSpace(config?.ReceiptFooterText)
            ? "Thank you!"
            : config.ReceiptFooterText;

        var sb = new StringBuilder();

        sb.AppendLine(NarrowCenter(firmName));
        if (!string.IsNullOrWhiteSpace(firmPhone))
            sb.AppendLine(NarrowCenter($"Ph: {firmPhone}"));
        if (!string.IsNullOrWhiteSpace(firmGSTIN))
            sb.AppendLine(NarrowCenter($"GSTIN: {firmGSTIN}"));
        sb.AppendLine(NarrowDivider());

        sb.AppendLine($"Inv: {sale.InvoiceNumber}");
        sb.AppendLine($"{regional.FormatDate(sale.SaleDate)} {regional.FormatTime(sale.SaleDate)}");
        sb.AppendLine(NarrowDivider());

        sb.AppendLine(NarrowItemHeader());
        sb.AppendLine(NarrowDivider());

        foreach (var item in sale.Items)
        {
            var name = Truncate(item.Product?.Name ?? $"#{item.ProductId}", 14);
            sb.AppendLine(NarrowItemLine(name, item.Quantity, item.Subtotal));
        }
        sb.AppendLine(NarrowDivider());

        if (sale.DiscountAmount > 0)
            sb.AppendLine(NarrowTotalLine("Disc", -sale.DiscountAmount));

        sb.AppendLine(NarrowTotalLine("TOTAL", sale.TotalAmount));
        sb.AppendLine(NarrowDivider());

        sb.AppendLine($"Paid: {sale.PaymentMethod}");
        sb.AppendLine(NarrowDivider());

        if (!string.IsNullOrWhiteSpace(footerText))
            sb.AppendLine(NarrowCenter(footerText));

        return sb.ToString();
    }

    private static string NarrowCenter(string text)
    {
        if (text.Length >= NarrowWidth) return text;
        var pad = (NarrowWidth - text.Length) / 2;
        return text.PadLeft(pad + text.Length);
    }

    private static string NarrowDivider() => new('-', NarrowWidth);

    private static string NarrowItemHeader() =>
        $"{"Item",-14} {"Qty",4} {"Amt",10}";

    private static string NarrowItemLine(string name, int qty, decimal amount) =>
        $"{name,-14} {qty,4} {amount,10:N2}";

    private string NarrowTotalLine(string label, decimal amount) =>
        $"{label,-18} {regional.FormatCurrency(amount),13}";

    // ── A5 half-page invoice (#443) ──────────────────────────────

    private const int A5Width = 60;

    public async Task<string> GenerateA5InvoiceAsync(int saleId, CancellationToken ct = default)
    {
        using var scope = perf.BeginScope("ReceiptService.GenerateA5InvoiceAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var sale = await context.Sales
            .AsNoTracking()
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == saleId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Sale Id {saleId} not found.");

        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var firmName = config?.FirmName ?? "Store";
        var firmAddress = config?.Address ?? "";
        var firmPhone = config?.Phone ?? "";
        var firmGSTIN = config?.GSTNumber ?? "";

        var sb = new StringBuilder();

        sb.AppendLine(A5Center("TAX INVOICE"));
        sb.AppendLine(A5Divider('='));

        sb.AppendLine(firmName);
        if (!string.IsNullOrWhiteSpace(firmAddress))
            sb.AppendLine(firmAddress);
        if (!string.IsNullOrWhiteSpace(firmGSTIN))
            sb.AppendLine($"GSTIN: {firmGSTIN}");
        if (!string.IsNullOrWhiteSpace(firmPhone))
            sb.AppendLine($"Phone: {firmPhone}");
        sb.AppendLine(A5Divider('-'));

        if (sale.Customer is not null)
        {
            sb.AppendLine($"To: {sale.Customer.Name}");
            if (!string.IsNullOrWhiteSpace(sale.Customer.Phone))
                sb.AppendLine($"Ph: {sale.Customer.Phone}");
            if (!string.IsNullOrWhiteSpace(sale.Customer.GSTIN))
                sb.AppendLine($"GSTIN: {sale.Customer.GSTIN}");
        }
        else
        {
            sb.AppendLine("To: Walk-in Customer");
        }
        sb.AppendLine(A5Divider('-'));

        sb.AppendLine($"Invoice: {sale.InvoiceNumber,-25} Date: {regional.FormatDate(sale.SaleDate)}");
        sb.AppendLine($"Payment: {sale.PaymentMethod,-25} Time: {regional.FormatTime(sale.SaleDate)}");
        sb.AppendLine(A5Divider('='));

        // Item header
        sb.AppendLine($"{"#",-3} {"Item",-18} {"Qty",4} {"Rate",8} {"GST%",5} {"Total",10}");
        sb.AppendLine(A5Divider('-'));

        var sn = 1;
        foreach (var item in sale.Items)
        {
            var name = item.Product?.Name ?? $"#{item.ProductId}";
            var truncName = name.Length > 18 ? name[..17] + "…" : name;
            var total = item.Subtotal + item.TaxAmount;
            sb.AppendLine($"{sn++,-3} {truncName,-18} {item.Quantity,4} {item.UnitPrice,8:N2} {item.TaxRate,4:N1}% {total,10:N2}");
        }
        sb.AppendLine(A5Divider('-'));

        var subtotal = sale.Items.Sum(i => i.Subtotal);
        var totalTax = sale.Items.Sum(i => i.TaxAmount);

        sb.AppendLine(A5TotalLine("Subtotal", subtotal));
        sb.AppendLine(A5TotalLine("Tax", totalTax));

        if (sale.DiscountAmount > 0)
        {
            var discLabel = sale.DiscountType == DiscountType.Percentage
                ? $"Discount ({sale.DiscountValue}%)"
                : "Discount";
            sb.AppendLine(A5TotalLine(discLabel, -sale.DiscountAmount));
        }

        sb.AppendLine(A5Divider('='));
        sb.AppendLine(A5TotalLine("TOTAL", sale.TotalAmount));
        sb.AppendLine(A5Divider('='));

        sb.AppendLine();
        sb.AppendLine(A5Center("Computer generated invoice."));

        return sb.ToString();
    }

    private static string A5Center(string text)
    {
        if (text.Length >= A5Width) return text;
        var pad = (A5Width - text.Length) / 2;
        return text.PadLeft(pad + text.Length);
    }

    private static string A5Divider(char c) => new(c, A5Width);

    private string A5TotalLine(string label, decimal amount) =>
        $"{label,-40} {regional.FormatCurrency(amount),19}";

    // ── Delivery challan (#444) ──────────────────────────────────

    public async Task<string> GenerateDeliveryChallanAsync(int saleId, CancellationToken ct = default)
    {
        using var scope = perf.BeginScope("ReceiptService.GenerateDeliveryChallanAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var sale = await context.Sales
            .AsNoTracking()
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == saleId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Sale Id {saleId} not found.");

        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        var firmName = config?.FirmName ?? "Store";
        var firmAddress = config?.Address ?? "";
        var firmPhone = config?.Phone ?? "";

        var sb = new StringBuilder();

        sb.AppendLine(A4Center("DELIVERY CHALLAN"));
        sb.AppendLine(A4Divider('='));

        sb.AppendLine(firmName);
        if (!string.IsNullOrWhiteSpace(firmAddress))
            sb.AppendLine(firmAddress);
        if (!string.IsNullOrWhiteSpace(firmPhone))
            sb.AppendLine($"Phone: {firmPhone}");
        sb.AppendLine(A4Divider('-'));

        if (sale.Customer is not null)
        {
            sb.AppendLine($"Deliver To: {sale.Customer.Name}");
            if (!string.IsNullOrWhiteSpace(sale.Customer.Address))
                sb.AppendLine($"  Address: {sale.Customer.Address}");
            if (!string.IsNullOrWhiteSpace(sale.Customer.Phone))
                sb.AppendLine($"  Phone: {sale.Customer.Phone}");
        }
        else
        {
            sb.AppendLine("Deliver To: Walk-in Customer");
        }
        sb.AppendLine(A4Divider('-'));

        sb.AppendLine($"Ref Invoice: {sale.InvoiceNumber,-40} Date: {regional.FormatDate(sale.SaleDate)}");
        sb.AppendLine(A4Divider('='));

        // Items — no prices, only items and quantities
        sb.AppendLine($"{"#",-5} {"Item",-50} {"Qty",10}");
        sb.AppendLine(A4Divider('-'));

        var sn = 1;
        foreach (var item in sale.Items)
        {
            var name = item.Product?.Name ?? $"#{item.ProductId}";
            var truncName = name.Length > 50 ? name[..49] + "…" : name;
            sb.AppendLine($"{sn++,-5} {truncName,-50} {item.Quantity,10}");
        }
        sb.AppendLine(A4Divider('-'));

        sb.AppendLine($"{"Total Items:",-55} {sale.Items.Sum(i => i.Quantity),10}");
        sb.AppendLine(A4Divider('='));

        sb.AppendLine();
        sb.AppendLine($"{"Received By: _______________",-40} {"Date: _______________"}");
        sb.AppendLine();
        sb.AppendLine(A4Center("This is a computer generated challan."));

        return sb.ToString();
    }

    // ── QR code data (#129) ──────────────────────────────────────

    public async Task<string> GenerateQrCodeDataAsync(int saleId, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("ReceiptService.GenerateQrCodeDataAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var sale = await context.Sales
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == saleId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Sale Id {saleId} not found.");

        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);

        // UPI-style QR payload: GSTIN | Invoice | Date | Total
        return string.Join("|",
            config?.GSTNumber ?? "",
            sale.InvoiceNumber ?? "",
            sale.SaleDate.ToString("dd-MM-yyyy"),
            sale.TotalAmount.ToString("F2"));
    }

    // ── Invoice barcode data (#130) ──────────────────────────────

    public string GenerateBarcodeData(string invoiceNumber) => invoiceNumber;

    // ── Email invoice stub (#135) ────────────────────────────────

    public Task<bool> SendInvoiceEmailAsync(int saleId, string recipientEmail, CancellationToken ct = default)
    {
        // Stub — future SMTP / SendGrid integration.
        return Task.FromResult(false);
    }
}
