using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Modules.Backup.Services;
using StoreAssistantPro.Modules.MainShell.Services;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.Services;

public sealed class DashboardServiceLoadTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IReferenceDataCache _cache = new ReferenceDataCache();
    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly IBackupService _backupService = Substitute.For<IBackupService>();

    public DashboardServiceLoadTests()
    {
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new AppDbContext(_dbOptions)));
        _dbFactory = factory;
        _regional.Now.Returns(new DateTime(2026, 3, 6, 18, 0, 0));
        _backupService.GetLastBackupDateAsync(Arg.Any<CancellationToken>())
            .Returns(new DateTime(2026, 3, 5, 22, 0, 0));
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldBenefitFromCache_OnLargeSeededDataset()
    {
        await using (var seed = new AppDbContext(_dbOptions))
        {
            await LargeRetailDatasetSeeder.SeedAsync(seed, productCount: 800, saleCount: 1200, itemsPerSale: 4);
        }

        var sut = new DashboardService(_dbFactory, _regional, _backupService, _perf, _cache);

        var firstStopwatch = Stopwatch.StartNew();
        var first = await sut.GetSummaryAsync();
        firstStopwatch.Stop();

        var secondStopwatch = Stopwatch.StartNew();
        var second = await sut.GetSummaryAsync();
        secondStopwatch.Stop();

        Assert.True(first.TodayTransactions > 0);
        Assert.Equal(first.TodayTransactions, second.TodayTransactions);
        Assert.True(secondStopwatch.Elapsed <= firstStopwatch.Elapsed + TimeSpan.FromMilliseconds(25),
            $"Expected cached dashboard load to be no slower than cold load. Cold={firstStopwatch.Elapsed.TotalMilliseconds:F1}ms, Cached={secondStopwatch.Elapsed.TotalMilliseconds:F1}ms.");
    }

    public void Dispose()
    {
        using var db = new AppDbContext(_dbOptions);
        db.Database.EnsureDeleted();
    }
}
