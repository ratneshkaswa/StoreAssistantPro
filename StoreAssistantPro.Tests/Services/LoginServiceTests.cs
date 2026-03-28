using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Services;

namespace StoreAssistantPro.Tests.Services;

public sealed class LoginServiceTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

    private LoginService CreateSut()
    {
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new AppDbContext(_dbOptions)));

        return new LoginService(factory, _eventBus, _auditService, _perf, NullLogger<LoginService>.Instance);
    }

    [Fact]
    public async Task ValidatePinAsync_LegacyLockoutState_DoesNotBlockSuccessfulLogin()
    {
        await SeedUserAsync(
            pin: "1234",
            failedAttempts: 4,
            lockoutEndTime: DateTime.UtcNow.AddMinutes(15));

        var result = await CreateSut().ValidatePinAsync(UserType.Admin, "1234");

        Assert.True(result.Succeeded);

        await using var db = new AppDbContext(_dbOptions);
        var credential = await db.UserCredentials.SingleAsync(c => c.UserType == UserType.Admin);
        Assert.Equal(0, credential.FailedAttempts);
        Assert.Null(credential.LockoutEndTime);
    }

    [Fact]
    public async Task ValidatePinAsync_RepeatedFailures_DoNotCreateAttemptLimit()
    {
        await SeedUserAsync(pin: "1234");

        var sut = CreateSut();
        for (var i = 0; i < 10; i++)
        {
            var failed = await sut.ValidatePinAsync(UserType.Admin, "0000");
            Assert.False(failed.Succeeded);
            Assert.Equal("Invalid PIN.", failed.ErrorMessage);
        }

        var success = await sut.ValidatePinAsync(UserType.Admin, "1234");

        Assert.True(success.Succeeded);

        await using var db = new AppDbContext(_dbOptions);
        var credential = await db.UserCredentials.SingleAsync(c => c.UserType == UserType.Admin);
        Assert.Equal(0, credential.FailedAttempts);
        Assert.Null(credential.LockoutEndTime);
    }

    [Fact]
    public async Task ValidateMasterPinAsync_RepeatedFailures_DoNotBlockLaterSuccess()
    {
        await SeedMasterPinAsync("654321");

        var sut = CreateSut();
        for (var i = 0; i < 10; i++)
            Assert.False(await sut.ValidateMasterPinAsync("000000"));

        Assert.True(await sut.ValidateMasterPinAsync("654321"));
    }

    private async Task SeedUserAsync(string pin, int failedAttempts = 0, DateTime? lockoutEndTime = null)
    {
        await using var db = new AppDbContext(_dbOptions);
        db.UserCredentials.Add(new UserCredential
        {
            UserType = UserType.Admin,
            PinHash = PinHasher.Hash(pin),
            FailedAttempts = failedAttempts,
            LockoutEndTime = lockoutEndTime
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedMasterPinAsync(string pin)
    {
        await using var db = new AppDbContext(_dbOptions);
        db.AppConfigs.Add(new AppConfig
        {
            FirmName = "Test Store",
            MasterPinHash = PinHasher.Hash(pin)
        });
        await db.SaveChangesAsync();
    }

    public void Dispose()
    {
        using var db = new AppDbContext(_dbOptions);
        db.Database.EnsureDeleted();
    }
}
