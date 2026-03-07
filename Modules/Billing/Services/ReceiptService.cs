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

        return GenerateThermalReceipt(sale, firmName, firmAddress, firmPhone, firmGSTIN, "Thank you! Visit again!");
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

    // ── Formatting helpers ───────────────────────────────────────

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
}
