using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Users.Services;

namespace StoreAssistantPro.Tests.Services;

public sealed class UserServiceTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

    private UserService CreateSut()
    {
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new AppDbContext(_dbOptions)));

        return new UserService(factory, _perf);
    }

    [Fact]
    public async Task ExportUsersAsync_IgnoresLegacyLockoutState()
    {
        await using (var db = new AppDbContext(_dbOptions))
        {
            db.UserCredentials.Add(new UserCredential
            {
                UserType = UserType.Admin,
                PinHash = "legacy-hash",
                DisplayName = "Owner",
                Email = "owner@example.com",
                Phone = "9876543210",
                FailedAttempts = 5,
                LockoutEndTime = DateTime.UtcNow.AddMinutes(30)
            });
            await db.SaveChangesAsync();
        }

        var rows = await CreateSut().ExportUsersAsync();

        var row = Assert.Single(rows);
        Assert.Equal("Admin", row.UserType);
        Assert.Equal("Owner", row.DisplayName);
        Assert.True(row.IsActive);
    }

    public void Dispose()
    {
        using var db = new AppDbContext(_dbOptions);
        db.Database.EnsureDeleted();
    }
}
