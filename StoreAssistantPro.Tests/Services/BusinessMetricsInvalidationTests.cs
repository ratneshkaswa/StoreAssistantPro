using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Events;
using StoreAssistantPro.Modules.Debtors.Services;
using StoreAssistantPro.Modules.Expenses.Services;
using StoreAssistantPro.Modules.Inward.Services;
using StoreAssistantPro.Modules.Ironing.Services;
using StoreAssistantPro.Modules.Orders.Services;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.Services;

public sealed class BusinessMetricsInvalidationTests : IDisposable
{
    private readonly SqliteDbContextFactory _dbFactory = new();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IPerformanceMonitor _perf =
        new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

    public BusinessMetricsInvalidationTests()
        => _regional.Now.Returns(new DateTime(2026, 3, 27, 12, 0, 0));

    [Fact]
    public async Task CreateExpenseAsync_ShouldPublishBusinessDataChangedEvent()
    {
        var sut = new ExpenseService(_dbFactory, _regional, _perf, _eventBus);

        await sut.CreateAsync(new ExpenseDto(
            Date: new DateTime(2026, 3, 27),
            Category: "Utilities",
            Amount: 550m,
            PaymentMethod: "Cash",
            Description: "Electricity bill",
            CreatedBy: "admin"));

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<SalesDataChangedEvent>(e => e.Reason == "ExpenseCreated"));
    }

    [Fact]
    public async Task SetOrderStatusAsync_ShouldPublishBusinessDataChangedEvent()
    {
        await SeedOrderAsync();
        var sut = new OrderService(_dbFactory, _regional, _perf, _eventBus);

        await sut.SetStatusAsync(1, "Delivered");

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<SalesDataChangedEvent>(e => e.Reason == "OrderStatusUpdated"));
    }

    [Fact]
    public async Task RecordDebtorPaymentAsync_ShouldPublishBusinessDataChangedEvent()
    {
        await SeedDebtorAsync();
        var sut = new DebtorService(_dbFactory, _perf, _eventBus);

        await sut.RecordPaymentAsync(1, 125m);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<SalesDataChangedEvent>(e => e.Reason == "DebtorPaymentRecorded"));
    }

    [Fact]
    public async Task CreateInwardEntryAsync_ShouldPublishBusinessDataChangedEvent()
    {
        await SeedInwardDependenciesAsync();
        var sut = new InwardService(_dbFactory, _regional, _perf, NullLogger<InwardService>.Instance, _eventBus);

        await sut.CreateAsync(new InwardEntryDto(
            InwardDate: new DateTime(2026, 3, 27),
            VendorId: 1,
            TransportCharges: 75m,
            Notes: "Morning delivery",
            Parcels:
            [
                new InwardParcelDto(
                    VendorId: 1,
                    TransportCharge: 75m,
                    Description: "Main parcel",
                    Products:
                    [
                        new InwardProductDto(
                            ProductId: 1,
                            Quantity: 4,
                            ColourId: null,
                            SizeId: null,
                            PatternId: null,
                            VariantTypeId: null)
                    ])
            ]));

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<SalesDataChangedEvent>(e => e.Reason == "InwardEntryCreated"));
    }

    [Fact]
    public async Task MarkIroningPaidAsync_ShouldPublishBusinessDataChangedEvent()
    {
        await SeedIroningEntryAsync();
        var sut = new IroningService(_dbFactory, _regional, _perf, _eventBus);

        await sut.MarkPaidAsync(1);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<SalesDataChangedEvent>(e => e.Reason == "IroningEntryMarkedPaid"));
    }

    public void Dispose() => _dbFactory.Dispose();

    private async Task SeedOrderAsync()
    {
        await using var seed = _dbFactory.CreateContext();
        seed.Orders.Add(new Order
        {
            Id = 1,
            Date = new DateTime(2026, 3, 27),
            CustomerName = "Asha",
            ItemDescription = "Tailoring",
            Quantity = 2,
            Rate = 300m,
            Amount = 600m,
            Status = "Pending",
            DeliveryDate = new DateTime(2026, 3, 29),
            CreatedAt = _regional.Now
        });
        await seed.SaveChangesAsync();
    }

    private async Task SeedDebtorAsync()
    {
        await using var seed = _dbFactory.CreateContext();
        seed.Debtors.Add(new Debtor
        {
            Id = 1,
            Name = "Ravi",
            Phone = "9999999999",
            TotalAmount = 500m,
            PaidAmount = 100m,
            Date = new DateTime(2026, 3, 27),
            Note = "Monthly account"
        });
        await seed.SaveChangesAsync();
    }

    private async Task SeedInwardDependenciesAsync()
    {
        await using var seed = _dbFactory.CreateContext();
        seed.Vendors.Add(new Vendor
        {
            Id = 1,
            Name = "North Textiles",
            Phone = "9999999998",
            CreatedDate = _regional.Now,
            IsActive = true
        });
        seed.Products.Add(new Product
        {
            Id = 1,
            Name = "White Shirt",
            SalePrice = 500m,
            CostPrice = 250m,
            Quantity = 10,
            IsActive = true
        });
        await seed.SaveChangesAsync();
    }

    private async Task SeedIroningEntryAsync()
    {
        await using var seed = _dbFactory.CreateContext();
        seed.IroningEntries.Add(new IroningEntry
        {
            Id = 1,
            Date = new DateTime(2026, 3, 27),
            CustomerName = "Mina",
            Items = "Saree",
            Quantity = 2,
            Rate = 80m,
            Amount = 160m,
            IsPaid = false
        });
        await seed.SaveChangesAsync();
    }
}
