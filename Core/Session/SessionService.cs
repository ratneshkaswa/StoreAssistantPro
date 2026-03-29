using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Session;

public class SessionService(
    IDbContextFactory<AppDbContext> contextFactory,
    IAppStateService appState,
    IRegionalSettingsService regionalSettings,
    ILogger<SessionService> logger) : ISessionService
{
    private readonly Lock _sessionLock = new();

    public UserType CurrentUserType { get; private set; }
    public string FirmName { get; private set; } = string.Empty;
    public bool IsLoggedIn { get; private set; }

    public async Task LoginAsync(UserType userType, CancellationToken ct = default)
    {
        lock (_sessionLock)
        {
            if (IsLoggedIn)
            {
                logger.LogWarning(
                    "Login rejected — session already active for {CurrentUser}, attempted {NewUser}",
                    CurrentUserType, userType);
                throw new InvalidOperationException(
                    $"Cannot log in as {userType} — {CurrentUserType} is already logged in. Logout first.");
            }
        }

        await using var context = await contextFactory.CreateDbContextAsync(ct);
        var config = await context.AppConfigs.AsNoTracking().FirstOrDefaultAsync(ct);

        lock (_sessionLock)
        {
            FirmName = config?.FirmName ?? string.Empty;
            CurrentUserType = userType;
            IsLoggedIn = true;
        }

        appState.SetFirmInfo(FirmName);
        appState.SetCurrentUser(userType);
        appState.SetLoggedIn(true);
        appState.SetDefaultPinFlag(config?.IsDefaultAdminPin ?? false);

        regionalSettings.UpdateSettings(
            config?.CurrencySymbol ?? "₹",
            config?.DateFormat ?? "dd/MM/yyyy");

        logger.LogInformation("Session created for {UserType}", userType);
    }

    public async Task RefreshFirmNameAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        var config = await context.AppConfigs.AsNoTracking().FirstOrDefaultAsync(ct);

        lock (_sessionLock)
        {
            FirmName = config?.FirmName ?? string.Empty;
        }

        appState.SetFirmInfo(FirmName);
    }

    public void Logout()
    {
        UserType previousUser;

        lock (_sessionLock)
        {
            if (!IsLoggedIn)
            {
                logger.LogWarning("Logout called but no active session");
                return;
            }

            previousUser = CurrentUserType;
            CurrentUserType = default;
            FirmName = string.Empty;
            IsLoggedIn = false;
        }

        appState.Reset();

        logger.LogInformation("Session cleared for {UserType}", previousUser);
    }
}
