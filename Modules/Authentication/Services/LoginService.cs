using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Events;

namespace StoreAssistantPro.Modules.Authentication.Services;

public class LoginService(
    IDbContextFactory<AppDbContext> contextFactory,
    IEventBus eventBus,
    IPerformanceMonitor perf,
    ILogger<LoginService> logger) : ILoginService
{
    public async Task<LoginResult> ValidatePinAsync(UserType userType, string pin, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope($"LoginService.ValidatePinAsync({userType})");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var credential = await context.UserCredentials
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserType == userType, ct).ConfigureAwait(false);

        if (credential is null)
        {
            logger.LogWarning("Login attempt for non-existent user type {UserType}", userType);
            return LoginResult.Failed("User not found.");
        }

        if (PinHasher.Verify(pin, credential.PinHash))
        {
            logger.LogInformation("Login succeeded for {UserType}", userType);
            await eventBus.PublishAsync(new UserLoginSuccessEvent(userType, DateTime.UtcNow));
            return LoginResult.Success();
        }

        logger.LogWarning("Login failed for {UserType} — invalid PIN", userType);
        return LoginResult.Failed("Invalid PIN.");
    }

    public async Task<bool> ValidateMasterPinAsync(string pin, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("LoginService.ValidateMasterPinAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var config = await context.AppConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var isValid = config is not null && PinHasher.Verify(pin, config.MasterPinHash);

        if (!isValid)
            logger.LogWarning("Master PIN validation failed");
        else
            logger.LogInformation("Master PIN validated successfully");

        return isValid;
    }
}
