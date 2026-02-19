using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Firm.Services;

public class FirmService(IDbContextFactory<AppDbContext> contextFactory) : IFirmService
{
    public async Task<AppConfig?> GetFirmAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.AppConfigs.AsNoTracking().FirstOrDefaultAsync();
    }

    public async Task UpdateFirmAsync(string firmName, string address, string phone)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var config = await context.AppConfigs.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Firm configuration not found.");

        config.FirmName = firmName;
        config.Address = address;
        config.Phone = phone;

        await context.SaveChangesAsync();
    }
}
