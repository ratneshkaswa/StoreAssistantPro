using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Firm.Services;

public class FirmService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf) : IFirmService
{
    public async Task<FirmManagementSnapshot?> GetFirmAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("FirmService.GetFirmAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var config = await context.AppConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (config is null)
            return null;

        var settings = await context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return new FirmManagementSnapshot(
            config.FirmName,
            config.Address,
            config.State,
            config.Pincode,
            config.Phone,
            config.Email,
            config.GSTNumber,
            config.PANNumber,
            string.IsNullOrWhiteSpace(config.GstRegistrationType) ? "Regular" : config.GstRegistrationType,
            config.CompositionSchemeRate,
            config.StateCode,
            config.FinancialYearStartMonth,
            config.FinancialYearEndMonth,
            string.IsNullOrWhiteSpace(config.CurrencySymbol) ? "\u20B9" : config.CurrencySymbol,
            string.IsNullOrWhiteSpace(config.DateFormat) ? "dd/MM/yyyy" : config.DateFormat,
            string.IsNullOrWhiteSpace(config.NumberFormat) ? "Indian" : config.NumberFormat,
            settings?.DefaultTaxMode ?? "Exclusive",
            settings?.RoundingMethod ?? "None",
            settings?.NegativeStockAllowed ?? false,
            string.IsNullOrWhiteSpace(settings?.NumberToWordsLanguage) ? "English" : settings.NumberToWordsLanguage);
    }

    public async Task UpdateFirmAsync(FirmUpdateDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var _ = perf.BeginScope("FirmService.UpdateFirmAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var config = await context.AppConfigs.FirstOrDefaultAsync(ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Firm configuration not found.");

        var settings = await context.SystemSettings.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (settings is null)
        {
            settings = new SystemSettings();
            context.SystemSettings.Add(settings);
        }

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
        config.CurrencySymbol = string.IsNullOrWhiteSpace(dto.CurrencySymbol) ? "\u20B9" : dto.CurrencySymbol;
        config.FinancialYearStartMonth = dto.FinancialYearStartMonth;
        config.FinancialYearEndMonth = dto.FinancialYearEndMonth;
        config.DateFormat = string.IsNullOrWhiteSpace(dto.DateFormat) ? "dd/MM/yyyy" : dto.DateFormat;
        config.NumberFormat = string.IsNullOrWhiteSpace(dto.NumberFormat) ? "Indian" : dto.NumberFormat;

        settings.DefaultTaxMode = string.IsNullOrWhiteSpace(dto.DefaultTaxMode) ? "Exclusive" : dto.DefaultTaxMode.Trim();
        settings.RoundingMethod = string.IsNullOrWhiteSpace(dto.RoundingMethod) ? "None" : dto.RoundingMethod.Trim();
        settings.NegativeStockAllowed = dto.NegativeStockAllowed;
        settings.NumberToWordsLanguage = string.IsNullOrWhiteSpace(dto.NumberToWordsLanguage) ? "English" : dto.NumberToWordsLanguage.Trim();

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
