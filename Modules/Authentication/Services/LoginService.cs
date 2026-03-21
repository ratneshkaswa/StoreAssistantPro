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
    IAuditService auditService,
    IPerformanceMonitor perf,
    ILogger<LoginService> logger) : ILoginService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<LoginResult> ValidatePinAsync(UserType userType, string pin, CancellationToken ct = default)
    {
        using var scope = perf.BeginScope($"LoginService.ValidatePinAsync({userType})");
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

            // Audit: login (#297/#278)
            _ = auditService.LogLoginAsync(userType.ToString(), ct);

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

    private const int MaxMasterPinAttempts = 5;
    private static readonly TimeSpan MasterPinLockoutDuration = TimeSpan.FromMinutes(30);
    private static int _masterPinFailedAttempts;
    private static DateTime _masterPinLockoutEnd = DateTime.MinValue;
    private static readonly Lock _masterPinLock = new();

    public async Task<bool> ValidateMasterPinAsync(string pin, CancellationToken ct = default)
    {
        lock (_masterPinLock)
        {
            if (_masterPinLockoutEnd > DateTime.UtcNow)
            {
                var remaining = _masterPinLockoutEnd - DateTime.UtcNow;
                logger.LogWarning("Master PIN validation blocked — locked out for {Minutes}m", (int)remaining.TotalMinutes + 1);
                return false;
            }
        }

        using var _ = perf.BeginScope("LoginService.ValidateMasterPinAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var config = await context.AppConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var isValid = config is not null && PinHasher.Verify(pin, config.MasterPinHash);

        if (!isValid)
        {
            lock (_masterPinLock)
            {
                _masterPinFailedAttempts++;
                if (_masterPinFailedAttempts >= MaxMasterPinAttempts)
                {
                    _masterPinLockoutEnd = DateTime.UtcNow.Add(MasterPinLockoutDuration);
                    _masterPinFailedAttempts = 0;
                    logger.LogWarning("Master PIN locked out after {Max} failed attempts", MaxMasterPinAttempts);
                }
            }
            logger.LogWarning("Master PIN validation failed");
        }
        else
        {
            lock (_masterPinLock)
            {
                _masterPinFailedAttempts = 0;
                _masterPinLockoutEnd = DateTime.MinValue;
            }
            logger.LogInformation("Master PIN validated successfully");
        }

        return isValid;
    }
}
