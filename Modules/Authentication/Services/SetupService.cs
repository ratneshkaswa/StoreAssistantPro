using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Authentication.Services;

public class SetupService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf) : ISetupService
{
    public async Task InitializeAppAsync(
        string firmName, string adminPin, string managerPin, string userPin, string masterPin)
    {
        using var _ = perf.BeginScope("SetupService.InitializeAppAsync");
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        if (await context.AppConfigs.AnyAsync().ConfigureAwait(false))
            throw new InvalidOperationException("Application has already been initialized.");

        context.AppConfigs.Add(new AppConfig
        {
            FirmName = firmName,
            IsInitialized = true,
            MasterPinHash = PinHasher.Hash(masterPin)
        });

        context.UserCredentials.Add(new UserCredential
        {
            UserType = UserType.Admin,
            PinHash = PinHasher.Hash(adminPin)
        });

        context.UserCredentials.Add(new UserCredential
        {
            UserType = UserType.Manager,
            PinHash = PinHasher.Hash(managerPin)
        });

        context.UserCredentials.Add(new UserCredential
        {
            UserType = UserType.User,
            PinHash = PinHasher.Hash(userPin)
        });

        try
        {
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException(
                "Application was already initialized by another machine. Please restart the app.");
        }
    }
}
