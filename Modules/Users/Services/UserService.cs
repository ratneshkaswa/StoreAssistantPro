using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Users.Services;

public class UserService(
    IDbContextFactory<AppDbContext> contextFactory,
    IPerformanceMonitor perf) : IUserService
{
    public async Task<IEnumerable<UserCredential>> GetAllUsersAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("UserService.GetAllUsersAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.UserCredentials
            .AsNoTracking()
            .OrderBy(u => u.UserType)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task ChangePinAsync(UserType userType, string newPin, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPin);

        using var _ = perf.BeginScope($"UserService.ChangePinAsync({userType})");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var credential = await context.UserCredentials
            .FirstOrDefaultAsync(c => c.UserType == userType, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Credential for {userType} not found.");

        credential.PinHash = PinHasher.Hash(newPin);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
