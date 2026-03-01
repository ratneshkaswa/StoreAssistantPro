using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Intents;
using StoreAssistantPro.Core.Workflows;

namespace StoreAssistantPro.Tests.Services;

public class MicroFeedbackServiceTests : IDisposable
{
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private Func<PinAutoSubmittedEvent, Task>? _onPinSubmitted;
    private Func<ZeroClickActionExecutedEvent, Task>? _onZeroClickAction;

    private readonly MicroFeedbackService _sut;

    public MicroFeedbackServiceTests()
    {
        _eventBus.When(x => x.Subscribe(Arg.Any<Func<PinAutoSubmittedEvent, Task>>()))
            .Do(ci => _onPinSubmitted = ci.Arg<Func<PinAutoSubmittedEvent, Task>>());

        _eventBus.When(x => x.Subscribe(Arg.Any<Func<ZeroClickActionExecutedEvent, Task>>()))
            .Do(ci => _onZeroClickAction = ci.Arg<Func<ZeroClickActionExecutedEvent, Task>>());

        _sut = new MicroFeedbackService(
            _eventBus,
            NullLogger<MicroFeedbackService>.Instance);
    }

    public void Dispose() => _sut.Dispose();

    // ═══════════════════════════════════════════════════════════════
    // Constructor subscriptions
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_SubscribesToPinAutoSubmittedEvent()
    {
        _eventBus.Received(1).Subscribe(Arg.Any<Func<PinAutoSubmittedEvent, Task>>());
    }

    [Fact]
    public void Constructor_SubscribesToZeroClickActionEvent()
    {
        _eventBus.Received(1).Subscribe(Arg.Any<Func<ZeroClickActionExecutedEvent, Task>>());
    }

    // ═══════════════════════════════════════════════════════════════
    // ═══════════════════════════════════════════════════════════════
    // PinAutoSubmitted → MicroFeedbackEvent
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task PinSubmitted_PublishesMicroFeedback_PinPad()
    {
        var evt = new PinAutoSubmittedEvent("UserPin");

        await _onPinSubmitted!(evt);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<MicroFeedbackEvent>(e =>
                e.TargetId == "PinPad" &&
                e.Type == MicroFeedbackType.Success));
    }

    [Fact]
    public async Task PinSubmitted_MasterPin_LabelContainsPinType()
    {
        var evt = new PinAutoSubmittedEvent("MasterPin");

        await _onPinSubmitted!(evt);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<MicroFeedbackEvent>(e => e.Label.Contains("MasterPin")));
    }

    // ═══════════════════════════════════════════════════════════════
    // ZeroClickActionExecuted → MicroFeedbackEvent
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ZeroClickAction_PublishesMicroFeedback_ConfirmType()
    {
        var evt = new ZeroClickActionExecutedEvent(
            "AutoSelectVendor", "Selected matching vendor", DateTime.UtcNow);

        await _onZeroClickAction!(evt);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<MicroFeedbackEvent>(e =>
                e.TargetId == "AutoSelectVendor" &&
                e.Type == MicroFeedbackType.Confirm));
    }

    [Fact]
    public async Task ZeroClickAction_LabelMatchesDescription()
    {
        var evt = new ZeroClickActionExecutedEvent(
            "AutoApplyDiscount", "Applied bulk discount", DateTime.UtcNow);

        await _onZeroClickAction!(evt);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<MicroFeedbackEvent>(e => e.Label == "Applied bulk discount"));
    }

    // ═══════════════════════════════════════════════════════════════
    // Dispose cleanup
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Dispose_UnsubscribesPinSubmitted()
    {
        _sut.Dispose();

        _eventBus.Received(1).Unsubscribe(
            Arg.Any<Func<PinAutoSubmittedEvent, Task>>());
    }

    [Fact]
    public void Dispose_UnsubscribesZeroClickAction()
    {
        _sut.Dispose();

        _eventBus.Received(1).Unsubscribe(
            Arg.Any<Func<ZeroClickActionExecutedEvent, Task>>());
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        _sut.Dispose();
        _sut.Dispose();

        // Each event type should only be unsubscribed once
        _eventBus.Received(1).Unsubscribe(
            Arg.Any<Func<PinAutoSubmittedEvent, Task>>());
    }

    // ═══════════════════════════════════════════════════════════════
    // MicroFeedbackEvent record
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void MicroFeedbackEvent_RecordEquality()
    {
        var a = new MicroFeedbackEvent("Cart:LastRow", MicroFeedbackType.Success, "Added T-Shirt");
        var b = new MicroFeedbackEvent("Cart:LastRow", MicroFeedbackType.Success, "Added T-Shirt");

        Assert.Equal(a, b);
    }

    [Fact]
    public void MicroFeedbackEvent_DifferentTargets_NotEqual()
    {
        var a = new MicroFeedbackEvent("Cart:LastRow", MicroFeedbackType.Success, "Added");
        var b = new MicroFeedbackEvent("PinPad", MicroFeedbackType.Success, "Added");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void MicroFeedbackEvent_DifferentTypes_NotEqual()
    {
        var a = new MicroFeedbackEvent("Search", MicroFeedbackType.Success, "Done");
        var b = new MicroFeedbackEvent("Search", MicroFeedbackType.Confirm, "Done");

        Assert.NotEqual(a, b);
    }

    // ═══════════════════════════════════════════════════════════════
    // MicroFeedbackType enum coverage
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(MicroFeedbackType.Success)]
    [InlineData(MicroFeedbackType.Confirm)]
    public void MicroFeedbackType_AllValuesAreDefined(MicroFeedbackType type)
    {
        Assert.True(Enum.IsDefined(type));
    }
}
