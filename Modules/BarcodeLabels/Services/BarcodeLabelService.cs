using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;

namespace StoreAssistantPro.Modules.BarcodeLabels.Services;

public class BarcodeLabelService(IDbContextFactory<AppDbContext> contextFactory) : IBarcodeLabelService
{
    public async Task<IReadOnlyList<BarcodeLabelProduct>> GetProductsForLabelAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new BarcodeLabelProduct(
                p.Id,
                p.Name,
                p.Barcode,
                p.SalePrice,
                p.CostPrice,
                p.SKU,
                p.Category != null ? p.Category.Name : null,
                p.Brand != null ? p.Brand.Name : null))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<string> GetFirmNameAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        return config?.FirmName ?? "Store";
    }

    public async Task<IReadOnlyList<BarcodeLabelVariant>> GetVariantsForProductAsync(int productId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
            .Include(v => v.Size)
            .Include(v => v.Colour)
            .Where(v => v.ProductId == productId && v.IsActive)
            .OrderBy(v => v.Size != null ? v.Size.SortOrder : 0)
            .ThenBy(v => v.Colour != null ? v.Colour.Name : "")
            .Select(v => new BarcodeLabelVariant(
                v.Id,
                v.ProductId,
                v.Product!.Name,
                v.Size != null ? v.Size.Name : null,
                v.Colour != null ? v.Colour.Name : null,
                v.Barcode,
                v.AdditionalPrice))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<string> AutoGenerateBarcodeAsync(int productId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Id == productId, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Product Id {productId} not found.");

        if (!string.IsNullOrWhiteSpace(product.Barcode))
            return product.Barcode;

        // Generate EAN-13: 200 (in-store prefix) + 9-digit padded product ID + check digit
        var prefix = "200";
        var body = productId.ToString().PadLeft(9, '0');
        var raw = prefix + body;
        var checkDigit = CalculateEan13CheckDigit(raw);
        var barcode = raw + checkDigit;

        // Ensure uniqueness
        while (await context.Products.AnyAsync(p => p.Barcode == barcode, ct).ConfigureAwait(false))
        {
            var rnd = Random.Shared.Next(100000000, 999999999);
            raw = prefix + rnd.ToString();
            checkDigit = CalculateEan13CheckDigit(raw);
            barcode = raw + checkDigit;
        }

        product.Barcode = barcode;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return barcode;
    }

    private static char CalculateEan13CheckDigit(string first12)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var digit = first12[i] - '0';
            sum += i % 2 == 0 ? digit : digit * 3;
        }
        var check = (10 - (sum % 10)) % 10;
        return (char)('0' + check);
    }

    // ── Price tag data (#446) ────────────────────────────────────────

    public async Task<IReadOnlyList<PriceTagData>> GetPriceTagDataAsync(IReadOnlyList<int> productIds, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => productIds.Contains(p.Id))
            .OrderBy(p => p.Name)
            .Select(p => new PriceTagData(
                p.Id,
                p.Name,
                p.SalePrice,
                p.CostPrice,
                p.Barcode,
                p.Category != null ? p.Category.Name : null,
                p.Brand != null ? p.Brand.Name : null,
                null, // SizeName — populated from variant if needed
                null)) // ColorName
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    // ── Barcode label config (#385/#386) ─────────────────────────────

    public async Task<BarcodeLabelConfig> GetLabelConfigAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        return new BarcodeLabelConfig(
            config?.BarcodeFormat ?? "EAN13",
            config?.LabelPaperSize ?? "65up");
    }
}
