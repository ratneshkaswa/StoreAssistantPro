using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Services;
using StoreAssistantPro.Modules.Billing.Services;

namespace StoreAssistantPro.Tests.Services;

public sealed class BillingServiceTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly ILoginService _loginService = Substitute.For<ILoginService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ICashRegisterService _cashRegisterService = Substitute.For<ICashRegisterService>();

    private BillingService CreateSut()
    {
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new AppDbContext(_dbOptions)));

        _regional.Now.Returns(new DateTime(2026, 3, 13, 10, 30, 0));

        return new BillingService(factory, _loginService, _auditService, _cashRegisterService, _perf, _regional);
    }

    [Fact]
    public async Task CompleteSaleAsync_NonCashWithoutReference_ThrowsMeaningfulError()
    {
        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CompleteSaleAsync(
            new CompleteSaleDto(
                Items: [new CartItemDto(11, null, 1, 999m, 0, 0, 0, false, 0)],
                PaymentMethod: "UPI",
                PaymentReference: null,
                DiscountType: DiscountType.None,
                DiscountValue: 0,
                DiscountReason: null,
                CashTendered: 0,
                CashierRole: "Admin",
                IdempotencyKey: Guid.NewGuid(),
                CustomerId: null)));

        Assert.Equal("Payment reference is required for non-cash sales.", ex.Message);
    }

    [Fact]
    public async Task CompleteSaleAsync_InvalidCartItemDiscount_ThrowsMeaningfulError()
    {
        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CompleteSaleAsync(
            new CompleteSaleDto(
                Items: [new CartItemDto(11, null, 1, 999m, 120, 0, 0, false, 0)],
                PaymentMethod: "Cash",
                PaymentReference: null,
                DiscountType: DiscountType.None,
                DiscountValue: 0,
                DiscountReason: null,
                CashTendered: 1000,
                CashierRole: "Admin",
                IdempotencyKey: Guid.NewGuid(),
                CustomerId: null)));

        Assert.Equal("Cart item discount must be between 0 and 100.", ex.Message);
    }

    public void Dispose()
    {
        using var db = new AppDbContext(_dbOptions);
        db.Database.EnsureDeleted();
    }
}
