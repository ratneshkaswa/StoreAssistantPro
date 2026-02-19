using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;

namespace StoreAssistantPro.Services;

public class StartupService(IDbContextFactory<AppDbContext> contextFactory) : IStartupService
{
    public async Task<bool> IsAppInitializedAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var config = await context.AppConfigs.FirstOrDefaultAsync();
        return config?.IsInitialized == true;
    }
}
