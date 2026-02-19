using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Helpers;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Services;

public class LoginService(IDbContextFactory<AppDbContext> contextFactory) : ILoginService
{
    public async Task<bool> ValidatePinAsync(UserType userType, string pin)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var credential = await context.UserCredentials
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserType == userType);

        return credential is not null && PinHasher.Verify(pin, credential.PinHash);
    }

    public async Task<bool> ValidateMasterPinAsync(string pin)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var config = await context.AppConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return config is not null && PinHasher.Verify(pin, config.MasterPinHash);
    }
}
