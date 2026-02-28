using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Firm.Services;

public class FirmService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf) : IFirmService
{
    public async Task<AppConfig?> GetFirmAsync()
    {
        using var _ = perf.BeginScope("FirmService.GetFirmAsync");
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.AppConfigs.AsNoTracking().FirstOrDefaultAsync().ConfigureAwait(false);
    }

    public async Task UpdateFirmAsync(string firmName, string address, string phone, string? gstNumber = null, string? currencyCode = null)
    {
        using var _ = perf.BeginScope("FirmService.UpdateFirmAsync");
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var config = await context.AppConfigs.FirstOrDefaultAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("Firm configuration not found.");

        config.FirmName = firmName;
        config.Address = address;
        config.Phone = phone;
        config.GSTNumber = string.IsNullOrWhiteSpace(gstNumber) ? null : gstNumber.Trim();
        if (!string.IsNullOrWhiteSpace(currencyCode))
            config.CurrencyCode = currencyCode.Trim();

        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}
