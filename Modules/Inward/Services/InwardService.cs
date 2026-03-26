using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Inward.Services;

public class InwardService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf,
    ILogger<InwardService> logger) : IInwardService
{
    private const int MaxParcelsPerEntry = 10;
    private const int MaxProductsPerParcel = 3;

    public async Task<IReadOnlyList<InwardEntry>> GetAllAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("InwardService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.InwardEntries
            .AsNoTracking()
            .AsSplitQuery()
            .Include(e => e.Vendor)
            .Include(e => e.Parcels)
                .ThenInclude(p => p.Products)
                    .ThenInclude(ip => ip.Product)
            .OrderByDescending(e => e.InwardDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<InwardEntry?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("InwardService.GetByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.InwardEntries
            .AsNoTracking()
            .AsSplitQuery()
            .Include(e => e.Vendor)
            .Include(e => e.Parcels)
                .ThenInclude(p => p.Vendor)
            .Include(e => e.Parcels)
                .ThenInclude(p => p.Products)
                    .ThenInclude(ip => ip.Product)
            .Include(e => e.Parcels)
                .ThenInclude(p => p.Products)
                    .ThenInclude(ip => ip.Colour)
            .Include(e => e.Parcels)
                .ThenInclude(p => p.Products)
                    .ThenInclude(ip => ip.Size)
            .Include(e => e.Parcels)
                .ThenInclude(p => p.Products)
                    .ThenInclude(ip => ip.Pattern)
            .Include(e => e.Parcels)
                .ThenInclude(p => p.Products)
                    .ThenInclude(ip => ip.VariantType)
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<string> GetNextParcelNumberAsync(DateTime date, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("InwardService.GetNextParcelNumberAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var nextSeq = await GetNextSequenceAsync(context, date, ct).ConfigureAwait(false);
        return FormatParcelNumber(date.Month, nextSeq);
    }

    public async Task<IReadOnlyList<string>> GenerateParcelNumbersAsync(DateTime date, int count, CancellationToken ct = default)
    {
        if (count < 1 || count > MaxParcelsPerEntry)
            throw new ArgumentOutOfRangeException(nameof(count), $"Parcel count must be 1–{MaxParcelsPerEntry}.");

        using var _ = perf.BeginScope("InwardService.GenerateParcelNumbersAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var nextSeq = await GetNextSequenceAsync(context, date, ct).ConfigureAwait(false);
        var numbers = new List<string>(count);
        for (var i = 0; i < count; i++)
            numbers.Add(FormatParcelNumber(date.Month, nextSeq + i));

        return numbers;
    }

    public async Task CreateAsync(InwardEntryDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateDto(dto);

        using var _ = perf.BeginScope("InwardService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var transaction = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        try
        {
            var now = regional.Now;
            var nextSeq = await GetNextSequenceAsync(context, dto.InwardDate, ct).ConfigureAwait(false);
            var inwardNumber = FormatParcelNumber(dto.InwardDate.Month, nextSeq);

            var entry = new InwardEntry
            {
                InwardNumber = inwardNumber,
                InwardDate = dto.InwardDate,
                VendorId = dto.VendorId,
                ParcelCount = dto.Parcels.Count,
                TransportCharges = dto.TransportCharges,
                Notes = dto.Notes?.Trim(),
                CreatedAt = now
            };

            context.InwardEntries.Add(entry);

            for (var i = 0; i < dto.Parcels.Count; i++)
            {
                var parcelDto = dto.Parcels[i];
                var parcelNumber = FormatParcelNumber(dto.InwardDate.Month, nextSeq + i);

                var parcel = new InwardParcel
                {
                    InwardEntry = entry,
                    ParcelNumber = parcelNumber,
                    VendorId = parcelDto.VendorId ?? dto.VendorId,
                    TransportCharge = parcelDto.TransportCharge,
                    Description = parcelDto.Description?.Trim()
                };

                context.InwardParcels.Add(parcel);

                foreach (var productDto in parcelDto.Products)
                {
                    if (productDto.Quantity <= 0)
                        throw new InvalidOperationException("Product quantity must be greater than zero.");

                    var inwardProduct = new InwardProduct
                    {
                        InwardParcel = parcel,
                        ProductId = productDto.ProductId,
                        Quantity = productDto.Quantity,
                        ColourId = productDto.ColourId,
                        SizeId = productDto.SizeId,
                        PatternId = productDto.PatternId,
                        VariantTypeId = productDto.VariantTypeId
                    };

                    context.InwardProducts.Add(inwardProduct);
                }
            }

            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            await transaction.CommitAsync(ct).ConfigureAwait(false);

            logger.LogInformation(
                "Inward entry '{Number}' created with {ParcelCount} parcel(s) on {Date}",
                inwardNumber, dto.Parcels.Count, dto.InwardDate.ToShortDateString());
        }
        catch
        {
            await transaction.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    // ── Parcel numbering ─────────────────────────────────────────────

    /// <summary>
    /// Finds the highest sequence number for parcels in the given month+year
    /// and returns the next one. Format: MM-NN.
    /// Resets to 1 automatically each month.
    /// </summary>
    private static async Task<int> GetNextSequenceAsync(
        AppDbContext context, DateTime date, CancellationToken ct)
    {
        var monthPrefix = $"{date.Month:D2}-";
        var yearStart = new DateTime(date.Year, date.Month, 1);
        var yearEnd = yearStart.AddMonths(1);

        var parcelNumbers = await context.InwardParcels
            .AsNoTracking()
            .Where(p => p.ParcelNumber.StartsWith(monthPrefix)
                     && p.InwardEntry!.InwardDate >= yearStart
                     && p.InwardEntry!.InwardDate < yearEnd)
            .Select(p => p.ParcelNumber)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (parcelNumbers.Count == 0)
            return 1;

        var maxSeq = parcelNumbers
            .Select(n => int.TryParse(n.AsSpan(3), out var seq) ? seq : 0)
            .Max();

        return maxSeq + 1;
    }

    /// <summary>Formats a parcel number as MM-NN (e.g., 03-01, 03-12).</summary>
    private static string FormatParcelNumber(int month, int sequence) =>
        $"{month:D2}-{sequence:D2}";

    // ── Validation ───────────────────────────────────────────────────

    private static void ValidateDto(InwardEntryDto dto)
    {
        if (dto.Parcels.Count == 0)
            throw new InvalidOperationException("At least one parcel is required.");

        if (dto.Parcels.Count > MaxParcelsPerEntry)
            throw new InvalidOperationException($"Maximum {MaxParcelsPerEntry} parcels per inward entry.");

        foreach (var parcel in dto.Parcels)
        {
            if (parcel.Products.Count == 0)
                throw new InvalidOperationException("Each parcel must have at least one product.");

            if (parcel.Products.Count > MaxProductsPerParcel)
                throw new InvalidOperationException($"Maximum {MaxProductsPerParcel} products per parcel.");
        }
    }
}
