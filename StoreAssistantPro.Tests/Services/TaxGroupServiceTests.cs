using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Tax.Services;

namespace StoreAssistantPro.Tests.Services;

public sealed class TaxGroupServiceTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    private readonly IRegionalSettingsService _regionalSettings = Substitute.For<IRegionalSettingsService>();
    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

    public TaxGroupServiceTests()
    {
        _regionalSettings.Now.Returns(new DateTime(2026, 3, 15, 10, 0, 0));
    }

    private TaxGroupService CreateSut()
    {
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new AppDbContext(_dbOptions)));

        return new TaxGroupService(factory, _regionalSettings, _perf);
    }

    [Fact]
    public async Task CreateSlabAsync_ShouldRejectOverlappingPriceRange()
    {
        await using (var db = new AppDbContext(_dbOptions))
        {
            db.TaxGroups.Add(new TaxGroup
            {
                Id = 1,
                Name = "GST Garments",
                CreatedDate = _regionalSettings.Now
            });
            db.TaxSlabs.Add(new TaxSlab
            {
                Id = 11,
                TaxGroupId = 1,
                GSTPercent = 5m,
                CGSTPercent = 2.5m,
                SGSTPercent = 2.5m,
                IGSTPercent = 5m,
                PriceFrom = 0m,
                PriceTo = 1000m,
                EffectiveFrom = new DateTime(2026, 1, 1),
                EffectiveTo = null,
                IsActive = true,
                CreatedDate = _regionalSettings.Now
            });
            await db.SaveChangesAsync();
        }

        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateSlabAsync(new TaxSlabDto(
                1,
                12m,
                500m,
                1500m,
                new DateTime(2026, 1, 1),
                null)));

        Assert.Contains("overlaps", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateSlabAsync_ShouldRejectOverlapWithAnotherSlab()
    {
        await using (var db = new AppDbContext(_dbOptions))
        {
            db.TaxGroups.Add(new TaxGroup
            {
                Id = 1,
                Name = "GST Garments",
                CreatedDate = _regionalSettings.Now
            });
            db.TaxSlabs.AddRange(
                new TaxSlab
                {
                    Id = 11,
                    TaxGroupId = 1,
                    GSTPercent = 5m,
                    CGSTPercent = 2.5m,
                    SGSTPercent = 2.5m,
                    IGSTPercent = 5m,
                    PriceFrom = 0m,
                    PriceTo = 1000m,
                    EffectiveFrom = new DateTime(2026, 1, 1),
                    EffectiveTo = null,
                    IsActive = true,
                    CreatedDate = _regionalSettings.Now
                },
                new TaxSlab
                {
                    Id = 12,
                    TaxGroupId = 1,
                    GSTPercent = 12m,
                    CGSTPercent = 6m,
                    SGSTPercent = 6m,
                    IGSTPercent = 12m,
                    PriceFrom = 1001m,
                    PriceTo = TaxSlab.MaxPrice,
                    EffectiveFrom = new DateTime(2026, 1, 1),
                    EffectiveTo = null,
                    IsActive = true,
                    CreatedDate = _regionalSettings.Now
                });
            await db.SaveChangesAsync();
        }

        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpdateSlabAsync(12, new TaxSlabDto(
                1,
                12m,
                900m,
                TaxSlab.MaxPrice,
                new DateTime(2026, 1, 1),
                null)));

        Assert.Contains("overlaps", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateHsnCodeAsync_ShouldRejectNonDigitCodes()
    {
        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.CreateHSNCodeAsync(new HSNCodeDto("61A0", "T-shirts", HSNCategory.Garments)));

        Assert.Contains("digits", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        using var db = new AppDbContext(_dbOptions);
        db.Database.EnsureDeleted();
    }
}
