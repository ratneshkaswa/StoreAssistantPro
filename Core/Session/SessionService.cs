using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Session;

public class SessionService(
    IDbContextFactory<AppDbContext> contextFactory,
    IAppStateService appState) : ISessionService
{
    public UserType CurrentUserType { get; private set; }
    public string FirmName { get; private set; } = string.Empty;
    public bool IsLoggedIn { get; private set; }

    public async Task LoginAsync(UserType userType)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var config = await context.AppConfigs.AsNoTracking().FirstOrDefaultAsync();

        FirmName = config?.FirmName ?? string.Empty;
        CurrentUserType = userType;
        IsLoggedIn = true;

        appState.SetFirmInfo(FirmName);
        appState.SetCurrentUser(userType);
        appState.SetLoggedIn(true);
    }

    public async Task RefreshFirmNameAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var config = await context.AppConfigs.AsNoTracking().FirstOrDefaultAsync();
        FirmName = config?.FirmName ?? string.Empty;

        appState.SetFirmInfo(FirmName);
    }

    public void Logout()
    {
        CurrentUserType = default;
        FirmName = string.Empty;
        IsLoggedIn = false;

        appState.SetCurrentUser(default);
        appState.SetLoggedIn(false);
    }
}
