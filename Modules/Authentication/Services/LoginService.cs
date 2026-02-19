using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Events;

namespace StoreAssistantPro.Modules.Authentication.Services;

public class LoginService(
    IDbContextFactory<AppDbContext> contextFactory,
    IEventBus eventBus) : ILoginService
{
    private const int MaxFailedAttempts = 3;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(2);

    public async Task<LoginResult> ValidatePinAsync(UserType userType, string pin)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var credential = await context.UserCredentials
            .FirstOrDefaultAsync(c => c.UserType == userType);

        if (credential is null)
            return LoginResult.Failed("User not found.", 0);

        var now = DateTime.UtcNow;

        // Active lockout — reject immediately
        if (credential.LockoutEndTime is not null && credential.LockoutEndTime > now)
        {
            await eventBus.PublishAsync(new UserLockedOutEvent(
                userType, now, credential.LockoutEndTime.Value));
            return LoginResult.LockedOut(credential.LockoutEndTime.Value);
        }

        // Expired lockout — reset counters
        if (credential.LockoutEndTime is not null)
        {
            credential.FailedAttempts = 0;
            credential.LockoutEndTime = null;
        }

        if (PinHasher.Verify(pin, credential.PinHash))
        {
            credential.FailedAttempts = 0;
            credential.LockoutEndTime = null;
            await context.SaveChangesAsync();

            await eventBus.PublishAsync(new UserLoginSuccessEvent(userType, now));
            return LoginResult.Success();
        }

        // Wrong PIN — increment and check threshold
        credential.FailedAttempts++;

        if (credential.FailedAttempts >= MaxFailedAttempts)
        {
            credential.LockoutEndTime = now.Add(LockoutDuration);
            await context.SaveChangesAsync();

            await eventBus.PublishAsync(new UserLockedOutEvent(
                userType, now, credential.LockoutEndTime.Value));
            return LoginResult.LockedOut(credential.LockoutEndTime.Value);
        }

        await context.SaveChangesAsync();
        var remaining = MaxFailedAttempts - credential.FailedAttempts;

        await eventBus.PublishAsync(new UserLoginFailedEvent(
            userType, now, credential.FailedAttempts, remaining));
        return LoginResult.Failed(
            $"Invalid PIN. {remaining} attempt(s) remaining.", remaining);
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
