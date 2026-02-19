using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Services;

public class SetupService(IDbContextFactory<AppDbContext> contextFactory) : ISetupService
{
    public async Task InitializeAppAsync(
        string firmName, string adminPin, string managerPin, string userPin, string masterPin)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

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

        await context.SaveChangesAsync();
    }
}
