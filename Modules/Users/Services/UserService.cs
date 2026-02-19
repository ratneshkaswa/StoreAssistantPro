using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Data;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Users.Services;

public class UserService(IDbContextFactory<AppDbContext> contextFactory) : IUserService
{
    public async Task<IEnumerable<UserCredential>> GetAllUsersAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.UserCredentials
            .AsNoTracking()
            .OrderBy(u => u.UserType)
            .ToListAsync();
    }

    public async Task ChangePinAsync(UserType userType, string newPin)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var credential = await context.UserCredentials
            .FirstOrDefaultAsync(c => c.UserType == userType)
            ?? throw new InvalidOperationException($"Credential for {userType} not found.");

        credential.PinHash = PinHasher.Hash(newPin);
        await context.SaveChangesAsync();
    }
}
