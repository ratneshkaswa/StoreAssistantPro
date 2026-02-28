using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Session;

public class SessionService(
    IDbContextFactory<AppDbContext> contextFactory,
    IAppStateService appState,
    ILogger<SessionService> logger) : ISessionService
{
    private readonly Lock _sessionLock = new();

    public UserType CurrentUserType { get; private set; }
    public string FirmName { get; private set; } = string.Empty;
    public bool IsLoggedIn { get; private set; }

    public async Task LoginAsync(UserType userType)
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

        await using var context = await contextFactory.CreateDbContextAsync();
        var config = await context.AppConfigs.AsNoTracking().FirstOrDefaultAsync();

        lock (_sessionLock)
        {
            FirmName = config?.FirmName ?? string.Empty;
            CurrentUserType = userType;
            IsLoggedIn = true;
        }

        appState.SetFirmInfo(FirmName);
        appState.SetCurrentUser(userType);
        appState.SetLoggedIn(true);

        logger.LogInformation("Session created for {UserType}", userType);
    }

    public async Task RefreshFirmNameAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var config = await context.AppConfigs.AsNoTracking().FirstOrDefaultAsync();

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
