using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Sales.Commands;
using StoreAssistantPro.Modules.Sales.Events;
using StoreAssistantPro.Modules.Sales.Services;

namespace StoreAssistantPro.Tests.Commands;

public class CompleteSaleHandlerTests
{
    private readonly ISalesService _salesService = Substitute.For<ISalesService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();

    public CompleteSaleHandlerTests()
    {
        _regional.Now.Returns(new DateTime(2026, 2, 19, 14, 30, 0));
    }

    private CompleteSaleHandler CreateSut() => new(_salesService, _eventBus, _regional);

    [Fact]
    public async Task HandleAsync_Success_CreatesSaleAndPublishesEvent()
    {
        var items = new List<SaleItemDto> { new(1, 3, 10m) };
        var command = new CompleteSaleCommand(30m, "Cash", items);

        var result = await CreateSut().HandleAsync(command);

        Assert.True(result.Succeeded);
        await _salesService.Received(1).CreateSaleAsync(Arg.Is<Models.Sale>(s =>
            s.TotalAmount == 30m &&
            s.PaymentMethod == "Cash" &&
            s.Items.Count == 1));
        await _eventBus.Received(1).PublishAsync(Arg.Is<SaleCompletedEvent>(e =>
            e.TotalAmount == 30m));
    }

    [Fact]
    public async Task HandleAsync_ServiceThrows_ReturnsFailure()
    {
        _salesService.CreateSaleAsync(Arg.Any<Models.Sale>())
            .Returns(Task.FromException(new InvalidOperationException("Stock insufficient")));

        var command = new CompleteSaleCommand(10m, "Card", [new(1, 1, 10m)]);

        var result = await CreateSut().HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal("Stock insufficient", result.ErrorMessage);
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<SaleCompletedEvent>());
    }
}
