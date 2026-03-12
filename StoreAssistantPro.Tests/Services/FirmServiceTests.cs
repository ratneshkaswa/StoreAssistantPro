using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Firm.Services;

namespace StoreAssistantPro.Tests.Services;

public class FirmServiceTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

    private IFirmService CreateSut()
    {
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new AppDbContext(_dbOptions)));

        return new FirmService(factory, _perf);
    }

    [Fact]
    public async Task GetFirmAsync_ReturnsCombinedFirmAndSystemSettings()
    {
        await using (var db = new AppDbContext(_dbOptions))
        {
            db.AppConfigs.Add(new AppConfig
            {
                FirmName = "Sonali Collection",
                Address = "12 Station Road",
                State = "Rajasthan",
                Pincode = "302001",
                Phone = "9876543210",
                Email = "firm@example.com",
                IsInitialized = true,
                MasterPinHash = "hash",
                GSTNumber = "08AAPFU0939F1Z8",
                PANNumber = "AAPFU0939F",
                GstRegistrationType = "Composition",
                CompositionSchemeRate = 1.5m,
                StateCode = "08",
                CurrencySymbol = "Rs.",
                FinancialYearStartMonth = 4,
                FinancialYearEndMonth = 3,
                DateFormat = "yyyy-MM-dd",
                NumberFormat = "Indian"
            });
            db.SystemSettings.Add(new SystemSettings
            {
                DefaultTaxMode = "Inclusive",
                RoundingMethod = "NearestTen",
                NegativeStockAllowed = true,
                NumberToWordsLanguage = "Hindi"
            });
            await db.SaveChangesAsync();
        }

        var sut = CreateSut();

        var snapshot = await sut.GetFirmAsync();

        Assert.NotNull(snapshot);
        Assert.Equal("Sonali Collection", snapshot!.FirmName);
        Assert.Equal("08AAPFU0939F1Z8", snapshot.GSTNumber);
        Assert.Equal("Composition", snapshot.GstRegistrationType);
        Assert.Equal(1.5m, snapshot.CompositionSchemeRate);
        Assert.Equal("Rs.", snapshot.CurrencySymbol);
        Assert.Equal("Inclusive", snapshot.DefaultTaxMode);
        Assert.Equal("NearestTen", snapshot.RoundingMethod);
        Assert.True(snapshot.NegativeStockAllowed);
        Assert.Equal("Hindi", snapshot.NumberToWordsLanguage);
    }

    [Fact]
    public async Task UpdateFirmAsync_UpdatesAppConfigAndCreatesSystemSettingsWhenMissing()
    {
        await using (var db = new AppDbContext(_dbOptions))
        {
            db.AppConfigs.Add(new AppConfig
            {
                FirmName = "Old Store",
                Address = string.Empty,
                State = "Delhi",
                Pincode = string.Empty,
                Phone = string.Empty,
                Email = string.Empty,
                IsInitialized = true,
                MasterPinHash = "hash",
                GSTNumber = null,
                PANNumber = null
            });
            await db.SaveChangesAsync();
        }

        var sut = CreateSut();

        await sut.UpdateFirmAsync(new FirmUpdateDto(
            FirmName: "Updated Store",
            Address: "456 Oak Avenue",
            State: "Maharashtra",
            Pincode: "400001",
            Phone: "9876543210",
            Email: "info@updated.com",
            GSTNumber: "27AAPFU0939F1ZV",
            PANNumber: "AAPFU0939F",
            GstRegistrationType: "Composition",
            CompositionSchemeRate: 1.5m,
            StateCode: "27",
            FinancialYearStartMonth: 7,
            FinancialYearEndMonth: 6,
            CurrencySymbol: "Rs.",
            DateFormat: "yyyy-MM-dd",
            NumberFormat: "Indian",
            DefaultTaxMode: "Inclusive",
            RoundingMethod: "NearestFive",
            NegativeStockAllowed: true,
            NumberToWordsLanguage: "Hindi"));

        await using var verifyDb = new AppDbContext(_dbOptions);
        var config = await verifyDb.AppConfigs.SingleAsync();
        var settings = await verifyDb.SystemSettings.SingleAsync();

        Assert.Equal("Updated Store", config.FirmName);
        Assert.Equal("456 Oak Avenue", config.Address);
        Assert.Equal("Maharashtra", config.State);
        Assert.Equal("400001", config.Pincode);
        Assert.Equal("9876543210", config.Phone);
        Assert.Equal("info@updated.com", config.Email);
        Assert.Equal("27AAPFU0939F1ZV", config.GSTNumber);
        Assert.Equal("AAPFU0939F", config.PANNumber);
        Assert.Equal("Composition", config.GstRegistrationType);
        Assert.Equal(1.5m, config.CompositionSchemeRate);
        Assert.Equal("27", config.StateCode);
        Assert.Equal("Rs.", config.CurrencySymbol);
        Assert.Equal(7, config.FinancialYearStartMonth);
        Assert.Equal(6, config.FinancialYearEndMonth);
        Assert.Equal("yyyy-MM-dd", config.DateFormat);
        Assert.Equal("Indian", config.NumberFormat);

        Assert.Equal("Inclusive", settings.DefaultTaxMode);
        Assert.Equal("NearestFive", settings.RoundingMethod);
        Assert.True(settings.NegativeStockAllowed);
        Assert.Equal("Hindi", settings.NumberToWordsLanguage);
    }

    public void Dispose()
    {
        using var db = new AppDbContext(_dbOptions);
        db.Database.EnsureDeleted();
    }
}
