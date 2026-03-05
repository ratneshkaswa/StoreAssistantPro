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
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<LoginResult> ValidatePinAsync(UserType userType, string pin, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope($"LoginService.ValidatePinAsync({userType})");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Must track state — cannot use AsNoTracking
        var credential = await context.UserCredentials
            .FirstOrDefaultAsync(c => c.UserType == userType, ct).ConfigureAwait(false);

        if (credential is null)
        {
            logger.LogWarning("Login attempt for non-existent user type {UserType}", userType);
            return LoginResult.Failed("User not found.");
        }

        // Check lockout (uses UTC for infrastructure-level expiry comparison)
        if (credential.LockoutEndTime.HasValue && credential.LockoutEndTime.Value > DateTime.UtcNow)
        {
            var remaining = credential.LockoutEndTime.Value - DateTime.UtcNow;
            logger.LogWarning("Login blocked for {UserType} — locked out for {Minutes}m", userType, (int)remaining.TotalMinutes + 1);
            return LoginResult.Failed($"Account locked. Try again in {(int)remaining.TotalMinutes + 1} minute(s).");
        }

        if (PinHasher.Verify(pin, credential.PinHash))
        {
            // Reset on success
            if (credential.FailedAttempts > 0)
            {
                credential.FailedAttempts = 0;
                credential.LockoutEndTime = null;
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            logger.LogInformation("Login succeeded for {UserType}", userType);
            await eventBus.PublishAsync(new UserLoginSuccessEvent(userType, DateTime.UtcNow));
            return LoginResult.Success();
        }

        // Increment failed attempts
        credential.FailedAttempts++;
        if (credential.FailedAttempts >= MaxFailedAttempts)
        {
            credential.LockoutEndTime = DateTime.UtcNow.Add(LockoutDuration);
            credential.FailedAttempts = 0;
            logger.LogWarning("Login locked out for {UserType} after {Max} failed attempts", userType, MaxFailedAttempts);
        }
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        var attemptsLeft = MaxFailedAttempts - credential.FailedAttempts;
        logger.LogWarning("Login failed for {UserType} — invalid PIN ({Left} attempts remaining)", userType, attemptsLeft);

        return credential.LockoutEndTime.HasValue
            ? LoginResult.Failed($"Account locked for {(int)LockoutDuration.TotalMinutes} minutes.")
            : LoginResult.Failed(attemptsLeft <= 2
                ? $"Invalid PIN. {attemptsLeft} attempt(s) remaining."
                : "Invalid PIN.");
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
