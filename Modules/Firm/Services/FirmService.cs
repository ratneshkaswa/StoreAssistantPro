using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Firm.Services;

public class FirmService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf) : IFirmService
{
    public async Task<AppConfig?> GetFirmAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("FirmService.GetFirmAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.AppConfigs.AsNoTracking().FirstOrDefaultAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateFirmAsync(FirmUpdateDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("FirmService.UpdateFirmAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Firm configuration not found.");

        config.FirmName = dto.FirmName;
        config.Address = dto.Address;
        config.State = dto.State;
        config.Pincode = dto.Pincode;
        config.Phone = dto.Phone;
        config.Email = dto.Email;
        config.GSTNumber = string.IsNullOrWhiteSpace(dto.GSTNumber) ? null : dto.GSTNumber.Trim();
        config.PANNumber = string.IsNullOrWhiteSpace(dto.PANNumber) ? null : dto.PANNumber.Trim();
        config.GstRegistrationType = string.IsNullOrWhiteSpace(dto.GstRegistrationType) ? "Regular" : dto.GstRegistrationType;
        config.CompositionSchemeRate = dto.CompositionSchemeRate;
        config.StateCode = string.IsNullOrWhiteSpace(dto.StateCode) ? null : dto.StateCode;
        config.CurrencySymbol = string.IsNullOrWhiteSpace(dto.CurrencySymbol) ? "₹" : dto.CurrencySymbol;
        config.FinancialYearStartMonth = dto.FinancialYearStartMonth;
        config.FinancialYearEndMonth = dto.FinancialYearEndMonth;
        config.DateFormat = dto.DateFormat;
        config.NumberFormat = dto.NumberFormat;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
