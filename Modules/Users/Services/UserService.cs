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
    public async Task<bool> HasUserRoleAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("UserService.HasUserRoleAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.UserCredentials
            .AsNoTracking()
            .AnyAsync(user => user.UserType == UserType.User, ct)
            .ConfigureAwait(false);
    }

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
        credential.FailedAttempts = 0;
        credential.LockoutEndTime = null;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ── User import (#287) ───────────────────────────────────────

    public async Task<int> ImportUsersAsync(IReadOnlyList<Dictionary<string, string>> rows, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0) return 0;

        using var _ = perf.BeginScope("UserService.ImportUsersAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var existingTypes = await context.UserCredentials
            .Select(u => u.UserType)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var count = 0;
        foreach (var row in rows)
        {
            var typeStr = (row.GetValueOrDefault("UserType") ?? row.GetValueOrDefault("Role") ?? "").Trim();
            var pin = (row.GetValueOrDefault("PIN") ?? row.GetValueOrDefault("Pin") ?? "1234").Trim();
            var name = (row.GetValueOrDefault("DisplayName") ?? row.GetValueOrDefault("Name") ?? "").Trim();

            if (!Enum.TryParse<UserType>(typeStr, true, out var userType)) continue;
            if (existingTypes.Contains(userType)) continue;

            context.UserCredentials.Add(new UserCredential
            {
                UserType = userType,
                PinHash = PinHasher.Hash(pin),
                DisplayName = string.IsNullOrWhiteSpace(name) ? null : name,
                Email = row.GetValueOrDefault("Email")?.Trim(),
                Phone = row.GetValueOrDefault("Phone")?.Trim()
            });

            existingTypes.Add(userType);
            count++;
        }

        if (count > 0)
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

        return count;
    }

    // ── User export (#288) ───────────────────────────────────────

    public async Task<IReadOnlyList<UserExportRow>> ExportUsersAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("UserService.ExportUsersAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var users = await context.UserCredentials
            .AsNoTracking()
            .OrderBy(u => u.UserType)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return users.Select(u => new UserExportRow(
            u.UserType.ToString(),
            u.DisplayName,
            u.LockoutEndTime == null || u.LockoutEndTime < DateTime.UtcNow,
            u.Email,
            u.Phone)).ToList();
    }

    // ── User profile (#281) ──────────────────────────────────────

    public async Task UpdateProfileAsync(UserType userType, string? displayName, string? email, string? phone, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("UserService.UpdateProfileAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var credential = await context.UserCredentials
            .FirstOrDefaultAsync(c => c.UserType == userType, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Credential for {userType} not found.");

        credential.DisplayName = displayName?.Trim();
        credential.Email = email?.Trim();
        credential.Phone = phone?.Trim();
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
