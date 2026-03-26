using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Authentication.Services;
using StoreAssistantPro.Modules.Billing.Services;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.Services;

public sealed class BillingServiceLoadTests : IDisposable
{
    private readonly SqliteDbContextFactory _dbFactory = new();
    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly ILoginService _loginService = Substitute.For<ILoginService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ICashRegisterService _cashRegisterService = Substitute.For<ICashRegisterService>();

    public BillingServiceLoadTests()
    {
        _regional.Now.Returns(new DateTime(2026, 3, 25, 11, 30, 0));
        _cashRegisterService.IsDayClosedAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(false);
    }

    [Fact]
    public async Task SearchProductsAsync_Should_HandleLargeSeededDataset()
    {
        await using (var seed = _dbFactory.CreateContext())
        {
            await LargeRetailDatasetSeeder.SeedAsync(seed, productCount: 800, saleCount: 0, itemsPerSale: 0);
        }

        var sut = new BillingService(_dbFactory, _loginService, _auditService, _cashRegisterService, _perf, _regional);

        var results = await sut.SearchProductsAsync("Load Product 01");

        Assert.NotEmpty(results);
        Assert.True(results.Count <= 20);
        Assert.All(results, product => Assert.Contains("Load Product 01", product.Name, StringComparison.Ordinal));
    }

    public void Dispose()
    {
        _dbFactory.Dispose();
    }
}
