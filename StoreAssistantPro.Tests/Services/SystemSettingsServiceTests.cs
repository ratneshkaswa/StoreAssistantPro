using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Settings.Services;

namespace StoreAssistantPro.Tests.Services;

public class SystemSettingsServiceTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

    private ISystemSettingsService CreateSut()
    {
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new AppDbContext(_dbOptions)));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=StoreAssistantPro_Test;"
            })
            .Build();

        return new SystemSettingsService(
            factory,
            configuration,
            _perf,
            NullLogger<SystemSettingsService>.Instance);
    }

    [Fact]
    public async Task GetAsync_WhenMissing_CreatesDefaultRowAndPreservesBusinessDefaults()
    {
        var sut = CreateSut();

        var settings = await sut.GetAsync();

        Assert.False(settings.AutoBackupEnabled);
        Assert.Equal("Exclusive", settings.DefaultTaxMode);
        Assert.Equal("None", settings.RoundingMethod);
        Assert.False(settings.NegativeStockAllowed);
        Assert.Equal("English", settings.NumberToWordsLanguage);

        await using var verifyDb = new AppDbContext(_dbOptions);
        Assert.Equal(1, await verifyDb.SystemSettings.CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOperationsFieldsOnly_LeavesBusinessRuleFieldsUnchanged()
    {
        await using (var db = new AppDbContext(_dbOptions))
        {
            db.SystemSettings.Add(new SystemSettings
            {
                BackupLocation = @"D:\OldBackups",
                AutoBackupEnabled = false,
                BackupTime = null,
                RestoreOption = "LastBackup",
                DefaultPrinter = "Old Printer",
                DefaultTaxMode = "Inclusive",
                RoundingMethod = "NearestTen",
                NegativeStockAllowed = true,
                NumberToWordsLanguage = "Hindi"
            });
            await db.SaveChangesAsync();
        }

        var sut = CreateSut();

        await sut.UpdateAsync(new SystemSettingsDto(
            BackupLocation: @"E:\NightlyBackups",
            AutoBackupEnabled: true,
            BackupTime: "23:30",
            RestoreOption: "SelectFile",
            DefaultPrinter: "Counter Printer"));

        await using var verifyDb = new AppDbContext(_dbOptions);
        var settings = await verifyDb.SystemSettings.SingleAsync();

        Assert.Equal(@"E:\NightlyBackups", settings.BackupLocation);
        Assert.True(settings.AutoBackupEnabled);
        Assert.Equal("23:30", settings.BackupTime);
        Assert.Equal("SelectFile", settings.RestoreOption);
        Assert.Equal("Counter Printer", settings.DefaultPrinter);

        Assert.Equal("Inclusive", settings.DefaultTaxMode);
        Assert.Equal("NearestTen", settings.RoundingMethod);
        Assert.True(settings.NegativeStockAllowed);
        Assert.Equal("Hindi", settings.NumberToWordsLanguage);
    }

    public void Dispose()
    {
        using var db = new AppDbContext(_dbOptions);
        db.Database.EnsureDeleted();
    }
}
