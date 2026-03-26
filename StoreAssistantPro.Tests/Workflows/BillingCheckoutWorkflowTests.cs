using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Services;
using StoreAssistantPro.Modules.Billing.Services;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.Workflows;

public sealed class BillingCheckoutWorkflowTests : IDisposable
{
    private readonly SqliteDbContextFactory _dbFactory = new();
    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly ILoginService _loginService = Substitute.For<ILoginService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ICashRegisterService _cashRegisterService = Substitute.For<ICashRegisterService>();

    public BillingCheckoutWorkflowTests()
    {
        _regional.Now.Returns(new DateTime(2026, 3, 25, 11, 30, 0));
        _cashRegisterService.IsDayClosedAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(false);
    }

    [Fact]
    public async Task CompleteSaleAsync_CashSale_PersistsSaleAndDeductsStock()
    {
        await using (var seed = _dbFactory.CreateContext())
        {
            seed.AppConfigs.Add(new AppConfig
            {
                InvoicePrefix = "BILL"
            });
            seed.Products.Add(new Product
            {
                Name = "Workflow Shirt",
                SalePrice = 100m,
                CostPrice = 60m,
                Quantity = 10,
                IsActive = true
            });
            await seed.SaveChangesAsync();
        }

        var sut = new BillingService(_dbFactory, _loginService, _auditService, _cashRegisterService, _perf, _regional);

        var sale = await sut.CompleteSaleAsync(new CompleteSaleDto(
            Items:
            [
                new CartItemDto(
                    ProductId: 1,
                    ProductVariantId: null,
                    Quantity: 2,
                    UnitPrice: 100m,
                    ItemDiscountRate: 5m,
                    ItemDiscountAmount: 0,
                    TaxRate: 5m,
                    IsTaxInclusive: false,
                    TaxAmount: 9.5m)
            ],
            PaymentMethod: "Cash",
            PaymentReference: null,
            DiscountType: DiscountType.None,
            DiscountValue: 0,
            DiscountReason: null,
            CashTendered: 200m,
            CashierRole: "Admin",
            IdempotencyKey: Guid.NewGuid(),
            CustomerId: null));

        Assert.StartsWith("BILL-20260325-", sale.InvoiceNumber, StringComparison.Ordinal);
        Assert.Equal(190m, sale.TotalAmount);
        Assert.Single(sale.Payments);
        Assert.Equal("Cash", sale.Payments.Single().Method);

        await using var verify = _dbFactory.CreateContext();
        var storedSale = await verify.Sales
            .Include(s => s.Items)
            .Include(s => s.Payments)
            .SingleAsync();
        var product = await verify.Products.SingleAsync();

        Assert.Equal(190m, storedSale.TotalAmount);
        Assert.Single(storedSale.Items);
        Assert.Single(storedSale.Payments);
        Assert.Equal(8, product.Quantity);
    }

    [Fact]
    public async Task CompleteSaleAsync_SplitCreditSale_CreatesDebtorAndCustomerRollup()
    {
        await using (var seed = _dbFactory.CreateContext())
        {
            seed.Products.Add(new Product
            {
                Name = "Workflow Saree",
                SalePrice = 200m,
                CostPrice = 120m,
                Quantity = 5,
                IsActive = true
            });
            seed.Customers.Add(new Customer
            {
                Name = "Anika",
                Phone = "9999999999"
            });
            await seed.SaveChangesAsync();
        }

        var sut = new BillingService(_dbFactory, _loginService, _auditService, _cashRegisterService, _perf, _regional);

        var sale = await sut.CompleteSaleAsync(new CompleteSaleDto(
            Items:
            [
                new CartItemDto(
                    ProductId: 1,
                    ProductVariantId: null,
                    Quantity: 1,
                    UnitPrice: 200m,
                    ItemDiscountRate: 0,
                    ItemDiscountAmount: 0,
                    TaxRate: 12m,
                    IsTaxInclusive: false,
                    TaxAmount: 24m)
            ],
            PaymentMethod: "Split",
            PaymentReference: null,
            DiscountType: DiscountType.None,
            DiscountValue: 0,
            DiscountReason: null,
            CashTendered: 120m,
            CashierRole: "Cashier",
            IdempotencyKey: Guid.NewGuid(),
            CustomerId: 1,
            SplitPayments:
            [
                new PaymentLegDto("Cash", 120m, null),
                new PaymentLegDto("Credit", 80m, null)
            ]));

        Assert.Equal("Split", sale.PaymentMethod);
        Assert.Equal(2, sale.Payments.Count);

        await using var verify = _dbFactory.CreateContext();
        var debtor = await verify.Debtors.SingleAsync();
        var customer = await verify.Customers.SingleAsync();
        var storedSale = await verify.Sales.Include(s => s.Payments).SingleAsync();

        Assert.Equal(80m, debtor.TotalAmount);
        Assert.Equal("Credit sale " + storedSale.InvoiceNumber, debtor.Note);
        Assert.Equal(200m, customer.TotalPurchaseAmount);
        Assert.Equal(1, customer.VisitCount);
        Assert.Equal(2, storedSale.Payments.Count);
    }

    public void Dispose()
    {
        _dbFactory.Dispose();
    }
}
