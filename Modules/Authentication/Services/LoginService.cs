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
    IRegionalSettingsService regional,
    IPerformanceMonitor perf,
    ILogger<LoginService> logger) : ILoginService
{
    private const int MaxFailedAttempts = 3;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(2);

    public async Task<LoginResult> ValidatePinAsync(UserType userType, string pin, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope($"LoginService.ValidatePinAsync({userType})");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var credential = await context.UserCredentials
            .FirstOrDefaultAsync(c => c.UserType == userType, ct).ConfigureAwait(false);

        if (credential is null)
        {
            logger.LogWarning("Login attempt for non-existent user type {UserType}", userType);
            return LoginResult.Failed("User not found.", 0);
        }

        var now = DateTime.UtcNow;

        // Active lockout — reject immediately
        if (credential.LockoutEndTime is not null && credential.LockoutEndTime > now)
        {
            logger.LogWarning("Login rejected — {UserType} is locked out until {LockoutEnd:u}",
                userType, credential.LockoutEndTime.Value);
            await eventBus.PublishAsync(new UserLockedOutEvent(
                userType, now, credential.LockoutEndTime.Value));
            return LoginResult.LockedOut(regional.FormatTime(credential.LockoutEndTime.Value));
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
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

            logger.LogInformation("Login succeeded for {UserType}", userType);
            await eventBus.PublishAsync(new UserLoginSuccessEvent(userType, now));
            return LoginResult.Success();
        }

        // Wrong PIN — increment and check threshold
        credential.FailedAttempts++;

        if (credential.FailedAttempts >= MaxFailedAttempts)
        {
            credential.LockoutEndTime = now.Add(LockoutDuration);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

            logger.LogWarning("Login failed — {UserType} locked out after {Attempts} attempts until {LockoutEnd:u}",
                userType, credential.FailedAttempts, credential.LockoutEndTime.Value);
            await eventBus.PublishAsync(new UserLockedOutEvent(
                userType, now, credential.LockoutEndTime.Value));
            return LoginResult.LockedOut(regional.FormatTime(credential.LockoutEndTime.Value));
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        var remaining = MaxFailedAttempts - credential.FailedAttempts;

        logger.LogWarning("Login failed for {UserType} — attempt {Attempts}/{MaxAttempts}, {Remaining} remaining",
            userType, credential.FailedAttempts, MaxFailedAttempts, remaining);
        await eventBus.PublishAsync(new UserLoginFailedEvent(
            userType, now, credential.FailedAttempts, remaining));
        return LoginResult.Failed(
            $"Invalid PIN. {remaining} attempt(s) remaining.", remaining);
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
