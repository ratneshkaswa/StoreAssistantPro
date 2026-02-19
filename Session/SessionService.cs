using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Session;

public class SessionService(IDbContextFactory<AppDbContext> contextFactory) : ISessionService
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
    }

    public void Logout()
    {
        IsLoggedIn = false;
    }
}
