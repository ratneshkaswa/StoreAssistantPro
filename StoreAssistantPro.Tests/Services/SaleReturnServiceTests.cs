using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Services;
using StoreAssistantPro.Modules.Billing.Events;
using StoreAssistantPro.Modules.Billing.Services;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.Services;

public sealed class SaleReturnServiceTests : IDisposable
{
    private readonly SqliteDbContextFactory _dbFactory = new();
    private readonly ILoginService _loginService = Substitute.For<ILoginService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ICashRegisterService _cashRegisterService = Substitute.For<ICashRegisterService>();
    private readonly IBillingService _billingService = Substitute.For<IBillingService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

    public SaleReturnServiceTests()
    {
        _regional.Now.Returns(new DateTime(2026, 3, 27, 11, 0, 0));
        _loginService.ValidateMasterPinAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _cashRegisterService.IsDayClosedAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(false);
    }

    [Fact]
    public async Task ProcessReturnAsync_ShouldRestoreStock_AndPublishDataChangedEvent()
    {
        await SeedSaleAsync();
        var sut = CreateSut();

        var result = await sut.ProcessReturnAsync(new SaleReturnDto(
            SaleId: 2,
            SaleItemId: 3,
            QuantityReturned: 1,
            Reason: "Damaged item",
            Notes: null,
            ApproverPin: "1234"));

        await using var verifyDb = _dbFactory.CreateContext();
        var product = await verifyDb.Products.SingleAsync(p => p.Id == 1);

        Assert.Equal(1, result.Quantity);
        Assert.Equal(100m, result.RefundAmount);
        Assert.Equal(11, product.Quantity);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<SalesDataChangedEvent>(e => e.Reason == "SaleReturnProcessed"));
    }

    [Fact]
    public async Task ExchangeAsync_ShouldPublishSingleDataChangedEvent()
    {
        await SeedSaleAsync();
        _billingService.CompleteSaleAsync(Arg.Any<CompleteSaleDto>(), Arg.Any<CancellationToken>())
            .Returns(new Sale
            {
                Id = 9,
                InvoiceNumber = "INV-EX-0001",
                SaleDate = new DateTime(2026, 3, 27, 11, 15, 0),
                TotalAmount = 220m,
                PaymentMethod = "Cash",
                IdempotencyKey = Guid.NewGuid()
            });

        var sut = CreateSut();

        var result = await sut.ExchangeAsync(new ExchangeDto(
            new SaleReturnDto(2, 3, 1, "Exchange", null, "1234"),
            new CompleteSaleDto(
                Items: [],
                PaymentMethod: "Cash",
                PaymentReference: null,
                DiscountType: DiscountType.None,
                DiscountValue: 0,
                DiscountReason: null,
                CashTendered: 220m,
                CashierRole: "Admin",
                IdempotencyKey: Guid.NewGuid(),
                CustomerId: null,
                SplitPayments: [])));

        Assert.Equal(100m, result.CreditApplied);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<SalesDataChangedEvent>(e => e.Reason == "SaleExchangeCompleted"));
    }

    public void Dispose() => _dbFactory.Dispose();

    private SaleReturnService CreateSut()
        => new(_dbFactory, _loginService, _auditService, _cashRegisterService, () => _billingService, _perf, _regional, _eventBus);

    private async Task SeedSaleAsync()
    {
        await using var seed = _dbFactory.CreateContext();
        seed.Products.Add(new Product
        {
            Id = 1,
            Name = "Blue Shirt",
            SalePrice = 250m,
            CostPrice = 125m,
            Quantity = 10,
            IsActive = true
        });
        seed.Sales.Add(new Sale
        {
            Id = 2,
            InvoiceNumber = "INV-20260327-0001",
            SaleDate = new DateTime(2026, 3, 27, 10, 30, 0),
            TotalAmount = 200m,
            PaymentMethod = "Cash",
            IdempotencyKey = Guid.NewGuid()
        });
        seed.SaleItems.Add(new SaleItem
        {
            Id = 3,
            SaleId = 2,
            ProductId = 1,
            Quantity = 2,
            UnitPrice = 100m,
            ItemDiscountRate = 0,
            ItemFlatDiscount = 0,
            TaxRate = 0,
            TaxAmount = 0,
            IsTaxInclusive = false
        });
        await seed.SaveChangesAsync();
    }
}
