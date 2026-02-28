using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Intents;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Events;
using StoreAssistantPro.Modules.Billing.Services;
using StoreAssistantPro.Modules.Products.Services;

namespace StoreAssistantPro.Tests.Services;

public class ZeroClickProductAddServiceTests : IDisposable
{
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly IBillingSessionService _billingSession = Substitute.For<IBillingSessionService>();
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();
    private readonly IPredictiveFocusService _focusService = Substitute.For<IPredictiveFocusService>();
    private readonly IPerformanceMonitor _perf;

    private Func<IntentDetectedEvent, Task>? _onIntentDetected;
    private readonly ZeroClickProductAddService _sut;

    public ZeroClickProductAddServiceTests()
    {
        _perf = new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);
        _billingSession.CurrentState.Returns(BillingSessionState.Active);
        _appState.IsOfflineMode.Returns(false);

        _eventBus.When(x => x.Subscribe(Arg.Any<Func<IntentDetectedEvent, Task>>()))
            .Do(ci => _onIntentDetected = ci.Arg<Func<IntentDetectedEvent, Task>>());

        _sut = new ZeroClickProductAddService(
            _eventBus, _productService, _billingSession, _appState,
            _focusService, _perf,
            NullLogger<ZeroClickProductAddService>.Instance);
    }

    public void Dispose() => _sut.Dispose();

    private Task RaiseIntent(IntentResult intent) =>
        _onIntentDetected!(new IntentDetectedEvent(intent));

    private static IntentResult MakeBarcode(string barcode, double confidence = 0.95) =>
        new()
        {
            Intent = InputIntent.BarcodeScan,
            Confidence = confidence,
            RawInput = barcode,
            Context = InputContext.BillingSearch,
            ResolvedValue = barcode
        };

    private static IntentResult MakeExactMatch(string text, double confidence = 0.95) =>
        new()
        {
            Intent = InputIntent.ExactProductMatch,
            Confidence = confidence,
            RawInput = text,
            Context = InputContext.BillingSearch
        };

    private static Product MakeProduct(int id = 1, string name = "Blue Shirt",
        decimal price = 499m, int qty = 10, string? barcode = "4006381333931") =>
        new()
        {
            Id = id, Name = name, SalePrice = price, Quantity = qty,
            Barcode = barcode, IsActive = true
        };

    // ═══════════════════════════════════════════════════════════════
    // Happy path: barcode scan → auto-add
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task BarcodeScan_SingleMatch_PublishesProductAddedEvent()
    {
        var product = MakeProduct();
        _productService.FindByBarcodeAsync("4006381333931", Arg.Any<CancellationToken>())
            .Returns([product]);

        await RaiseIntent(MakeBarcode("4006381333931"));

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ProductAddedToCartEvent>(e =>
                e.ProductId == 1 &&
                e.ProductName == "Blue Shirt" &&
                e.Quantity == 1 &&
                e.UnitPrice == 499m &&
                e.Source == "Barcode"));
    }

    [Fact]
    public async Task BarcodeScan_SingleMatch_RequestsFocusReturn()
    {
        _productService.FindByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([MakeProduct()]);

        await RaiseIntent(MakeBarcode("4006381333931"));

        _focusService.Received(1).RequestFocus(
            "BillingSearchBox",
            Arg.Is<string>(s => s.Contains("Barcode")));
    }

    // ═══════════════════════════════════════════════════════════════
    // Happy path: exact product match → auto-add
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExactMatch_SingleMatch_PublishesProductAddedEvent()
    {
        var product = MakeProduct(name: "Cotton Kurta", barcode: null);
        _productService.FindByExactTextAsync("Cotton Kurta", Arg.Any<CancellationToken>())
            .Returns([product]);

        await RaiseIntent(MakeExactMatch("Cotton Kurta"));

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ProductAddedToCartEvent>(e =>
                e.ProductName == "Cotton Kurta" &&
                e.Source == "ExactMatch"));
    }

    // ═══════════════════════════════════════════════════════════════
    // Rejection: multiple matches
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task BarcodeScan_MultipleMatches_DoesNotAdd()
    {
        _productService.FindByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([MakeProduct(1), MakeProduct(2)]);

        await RaiseIntent(MakeBarcode("4006381333931"));

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ProductAddedToCartEvent>());
    }

    [Fact]
    public async Task ExactMatch_MultipleMatches_DoesNotAdd()
    {
        _productService.FindByExactTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([MakeProduct(1, "Shirt A"), MakeProduct(2, "Shirt B")]);

        await RaiseIntent(MakeExactMatch("Shirt"));

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ProductAddedToCartEvent>());
    }

    // ═══════════════════════════════════════════════════════════════
    // Rejection: no matches
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task BarcodeScan_NoMatch_DoesNotAdd()
    {
        _productService.FindByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Product>());

        await RaiseIntent(MakeBarcode("9999999999999"));

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ProductAddedToCartEvent>());
    }

    // ═══════════════════════════════════════════════════════════════
    // Rejection: low barcode confidence
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task BarcodeScan_LowConfidence_DoesNotAdd()
    {
        _productService.FindByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([MakeProduct()]);

        await RaiseIntent(MakeBarcode("12345678", confidence: 0.7));

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ProductAddedToCartEvent>());
    }

    // ═══════════════════════════════════════════════════════════════
    // Safety: out of stock
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task BarcodeScan_OutOfStock_DoesNotAdd()
    {
        var product = MakeProduct(qty: 0);
        _productService.FindByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([product]);

        await RaiseIntent(MakeBarcode("4006381333931"));

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ProductAddedToCartEvent>());
    }

    // ═══════════════════════════════════════════════════════════════
    // Safety: no active billing session
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(BillingSessionState.None)]
    [InlineData(BillingSessionState.Completed)]
    [InlineData(BillingSessionState.Cancelled)]
    public async Task NoActiveBillingSession_DoesNotAdd(BillingSessionState state)
    {
        _billingSession.CurrentState.Returns(state);

        await RaiseIntent(MakeBarcode("4006381333931"));

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ProductAddedToCartEvent>());
    }

    // ═══════════════════════════════════════════════════════════════
    // Safety: offline mode
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task OfflineMode_DoesNotAdd()
    {
        _appState.IsOfflineMode.Returns(true);

        await RaiseIntent(MakeBarcode("4006381333931"));

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ProductAddedToCartEvent>());
    }

    // ═══════════════════════════════════════════════════════════════
    // Ignored intents
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task PinIntent_IsIgnored()
    {
        var pin = new IntentResult
        {
            Intent = InputIntent.PinCompleted,
            Confidence = 1.0,
            RawInput = "4829",
            Context = InputContext.PinEntry,
            ResolvedValue = "UserPin"
        };

        await RaiseIntent(pin);

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ProductAddedToCartEvent>());
    }

    [Fact]
    public async Task AutoCompleteIntent_IsIgnored()
    {
        var ac = new IntentResult
        {
            Intent = InputIntent.AutoCompleteTrigger,
            Confidence = 0.9,
            RawInput = "shi",
            Context = InputContext.BillingSearch
        };

        await RaiseIntent(ac);

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ProductAddedToCartEvent>());
    }

    // ═══════════════════════════════════════════════════════════════
    // Wrong context
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task PinContext_IsIgnored()
    {
        var intent = new IntentResult
        {
            Intent = InputIntent.BarcodeScan,
            Confidence = 0.95,
            RawInput = "4006381333931",
            Context = InputContext.PinEntry
        };

        await RaiseIntent(intent);

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ProductAddedToCartEvent>());
    }

    // ═══════════════════════════════════════════════════════════════
    // Event subscription lifecycle
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_SubscribesToIntentDetectedEvent()
    {
        _eventBus.Received(1).Subscribe(
            Arg.Any<Func<IntentDetectedEvent, Task>>());
    }

    [Fact]
    public void Dispose_UnsubscribesFromEvent()
    {
        _sut.Dispose();

        _eventBus.Received(1).Unsubscribe(
            Arg.Any<Func<IntentDetectedEvent, Task>>());
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        _sut.Dispose();
        _sut.Dispose();

        _eventBus.Received(1).Unsubscribe(
            Arg.Any<Func<IntentDetectedEvent, Task>>());
    }

    // ═══════════════════════════════════════════════════════════════
    // ProductAddedToCartEvent record
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Event_RecordEquality()
    {
        var a = new ProductAddedToCartEvent(1, "Shirt", 1, 499m, "Barcode");
        var b = new ProductAddedToCartEvent(1, "Shirt", 1, 499m, "Barcode");
        Assert.Equal(a, b);
    }

    // ═══════════════════════════════════════════════════════════════
    // Error resilience
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProductServiceThrows_DoesNotCrash()
    {
        _productService.FindByBarcodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<Product>>(_ => throw new InvalidOperationException("db error"));

        await RaiseIntent(MakeBarcode("4006381333931"));

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ProductAddedToCartEvent>());
    }
}
