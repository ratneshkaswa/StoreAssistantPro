using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Intents;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Tests.Services;

public class ZeroClickPinServiceTests : IDisposable
{
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IPredictiveFocusService _focusService = Substitute.For<IPredictiveFocusService>();
    private readonly IPerformanceMonitor _perf;

    private Func<IntentDetectedEvent, Task>? _onIntentDetected;
    private readonly ZeroClickPinService _sut;

    public ZeroClickPinServiceTests()
    {
        _perf = new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);

        _eventBus.When(x => x.Subscribe(Arg.Any<Func<IntentDetectedEvent, Task>>()))
            .Do(ci => _onIntentDetected = ci.Arg<Func<IntentDetectedEvent, Task>>());

        _sut = new ZeroClickPinService(
            _eventBus, _focusService, _perf,
            NullLogger<ZeroClickPinService>.Instance);
    }

    public void Dispose() => _sut.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────

    private Task RaiseIntent(IntentResult intent) =>
        _onIntentDetected!(new IntentDetectedEvent(intent));

    private static IntentResult MakeUserPin(string pin = "4829") => new()
    {
        Intent = InputIntent.PinCompleted,
        Confidence = 1.0,
        RawInput = pin,
        Context = InputContext.PinEntry,
        ResolvedValue = "UserPin"
    };

    private static IntentResult MakeMasterPin(string pin = "829417") => new()
    {
        Intent = InputIntent.PinCompleted,
        Confidence = 1.0,
        RawInput = pin,
        Context = InputContext.PinEntry,
        ResolvedValue = "MasterPin"
    };

    private static IZeroClickPinService.PinSubmitHandler SuccessHandler() =>
        (_, pinType) => Task.FromResult(PinSubmissionResult.Success(pinType));

    private static IZeroClickPinService.PinSubmitHandler FailureHandler(string error) =>
        (_, pinType) => Task.FromResult(PinSubmissionResult.Failure(pinType, error));

    private static IZeroClickPinService.PinSubmitHandler ThrowingHandler() =>
        (_, _) => throw new InvalidOperationException("db error");

    // ═══════════════════════════════════════════════════════════════
    // Happy path: 4-digit user PIN → auto-submit → success
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task UserPin_Success_PublishesPinAutoSubmittedEvent()
    {
        _sut.RegisterHandler(SuccessHandler(), "PinPad");

        await RaiseIntent(MakeUserPin());

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<PinAutoSubmittedEvent>(e => e.PinType == "UserPin"));
    }

    [Fact]
    public async Task UserPin_Success_DoesNotPublishFailedEvent()
    {
        _sut.RegisterHandler(SuccessHandler(), "PinPad");

        await RaiseIntent(MakeUserPin());

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<PinSubmissionFailedEvent>());
    }

    // ═══════════════════════════════════════════════════════════════
    // Happy path: 6-digit master PIN → auto-submit → success
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task MasterPin_Success_PublishesPinAutoSubmittedEvent()
    {
        _sut.RegisterHandler(SuccessHandler(), "MasterPinInput");

        await RaiseIntent(MakeMasterPin());

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<PinAutoSubmittedEvent>(e => e.PinType == "MasterPin"));
    }

    // ═══════════════════════════════════════════════════════════════
    // Failure: handler returns failure → clear PIN + inline error
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task UserPin_Failure_PublishesPinSubmissionFailedEvent()
    {
        _sut.RegisterHandler(FailureHandler("Invalid PIN. Try again."), "PinPad");

        await RaiseIntent(MakeUserPin());

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<PinSubmissionFailedEvent>(e =>
                e.PinType == "UserPin" &&
                e.ErrorMessage == "Invalid PIN. Try again."));
    }

    [Fact]
    public async Task UserPin_Failure_RequestsFocusReturn()
    {
        _sut.RegisterHandler(FailureHandler("Wrong PIN"), "PinPad");

        await RaiseIntent(MakeUserPin());

        _focusService.Received(1).RequestFocus(
            "PinPad",
            Arg.Is<string>(s => s.Contains("Failed")));
    }

    [Fact]
    public async Task UserPin_Failure_DoesNotPublishSuccessEvent()
    {
        _sut.RegisterHandler(FailureHandler("Wrong PIN"), "PinPad");

        await RaiseIntent(MakeUserPin());

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<PinAutoSubmittedEvent>());
    }

    // ═══════════════════════════════════════════════════════════════
    // No handler registered → skip silently
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task NoHandler_DoesNotPublishAnyEvent()
    {
        // No RegisterHandler call

        await RaiseIntent(MakeUserPin());

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<PinAutoSubmittedEvent>());
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<PinSubmissionFailedEvent>());
    }

    // ═══════════════════════════════════════════════════════════════
    // Non-PIN intent → ignored
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task BarcodeIntent_IsIgnored()
    {
        _sut.RegisterHandler(SuccessHandler(), "PinPad");

        var barcode = new IntentResult
        {
            Intent = InputIntent.BarcodeScan,
            Confidence = 0.95,
            RawInput = "4006381333931",
            Context = InputContext.BillingSearch
        };

        await RaiseIntent(barcode);

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<PinAutoSubmittedEvent>());
    }

    [Fact]
    public async Task AutoCompleteIntent_IsIgnored()
    {
        _sut.RegisterHandler(SuccessHandler(), "PinPad");

        var ac = new IntentResult
        {
            Intent = InputIntent.AutoCompleteTrigger,
            Confidence = 0.9,
            RawInput = "shi",
            Context = InputContext.ProductSearch
        };

        await RaiseIntent(ac);

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<PinAutoSubmittedEvent>());
    }

    // ═══════════════════════════════════════════════════════════════
    // Low confidence PIN → rejected by ConfidenceEvaluator
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task PinCompleted_WrongContext_Rejected()
    {
        _sut.RegisterHandler(SuccessHandler(), "PinPad");

        var intent = new IntentResult
        {
            Intent = InputIntent.PinCompleted,
            Confidence = 1.0,
            RawInput = "4829",
            Context = InputContext.General, // wrong context
            ResolvedValue = "UserPin"
        };

        await RaiseIntent(intent);

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<PinAutoSubmittedEvent>());
    }

    [Fact]
    public async Task PinCompleted_LowConfidence_Rejected()
    {
        _sut.RegisterHandler(SuccessHandler(), "PinPad");

        var intent = new IntentResult
        {
            Intent = InputIntent.PinCompleted,
            Confidence = 0.8, // not certain
            RawInput = "4829",
            Context = InputContext.PinEntry
        };

        await RaiseIntent(intent);

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<PinAutoSubmittedEvent>());
    }

    // ═══════════════════════════════════════════════════════════════
    // Double-submission guard
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task DoubleSubmission_IsBlocked()
    {
        var tcs = new TaskCompletionSource<PinSubmissionResult>();
        var callCount = 0;

        _sut.RegisterHandler((_, pinType) =>
        {
            Interlocked.Increment(ref callCount);
            return tcs.Task;
        }, "PinPad");

        // Fire first intent — handler blocks
        var first = RaiseIntent(MakeUserPin());

        // Fire second intent while first is still in progress
        await RaiseIntent(MakeUserPin("5678"));

        // Complete the first
        tcs.SetResult(PinSubmissionResult.Success("UserPin"));
        await first;

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task IsSubmitting_TrueDuringHandler()
    {
        var tcs = new TaskCompletionSource<PinSubmissionResult>();
        bool? submittingDuringHandler = null;

        _sut.RegisterHandler((_, pinType) =>
        {
            submittingDuringHandler = _sut.IsSubmitting;
            return tcs.Task;
        }, "PinPad");

        var task = RaiseIntent(MakeUserPin());
        tcs.SetResult(PinSubmissionResult.Success("UserPin"));
        await task;

        Assert.True(submittingDuringHandler);
        Assert.False(_sut.IsSubmitting); // reset after completion
    }

    // ═══════════════════════════════════════════════════════════════
    // Handler throws → publishes failed event, doesn't crash
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task HandlerThrows_PublishesFailedEvent()
    {
        _sut.RegisterHandler(ThrowingHandler(), "PinPad");

        await RaiseIntent(MakeUserPin());

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<PinSubmissionFailedEvent>(e =>
                e.ErrorMessage == "An unexpected error occurred."));
    }

    [Fact]
    public async Task HandlerThrows_ResetsIsSubmitting()
    {
        _sut.RegisterHandler(ThrowingHandler(), "PinPad");

        await RaiseIntent(MakeUserPin());

        Assert.False(_sut.IsSubmitting);
    }

    // ═══════════════════════════════════════════════════════════════
    // Handler registration / unregistration
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void RegisterHandler_SetsIsHandlerRegistered()
    {
        Assert.False(_sut.IsHandlerRegistered);

        _sut.RegisterHandler(SuccessHandler(), "PinPad");

        Assert.True(_sut.IsHandlerRegistered);
    }

    [Fact]
    public void UnregisterHandler_ClearsIsHandlerRegistered()
    {
        _sut.RegisterHandler(SuccessHandler(), "PinPad");
        _sut.UnregisterHandler();

        Assert.False(_sut.IsHandlerRegistered);
    }

    [Fact]
    public async Task UnregisteredHandler_SkipsProcessing()
    {
        _sut.RegisterHandler(SuccessHandler(), "PinPad");
        _sut.UnregisterHandler();

        await RaiseIntent(MakeUserPin());

        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<PinAutoSubmittedEvent>());
    }

    [Fact]
    public void RegisterHandler_NullHandler_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _sut.RegisterHandler(null!, "PinPad"));
    }

    [Fact]
    public void RegisterHandler_OverridesPrevious()
    {
        _sut.RegisterHandler(FailureHandler("first"), "Input1");
        _sut.RegisterHandler(SuccessHandler(), "Input2");

        Assert.True(_sut.IsHandlerRegistered);
    }

    // ═══════════════════════════════════════════════════════════════
    // PIN type inference from length
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task PinWithoutResolvedValue_InfersUserPin()
    {
        string? receivedType = null;
        _sut.RegisterHandler((_, pinType) =>
        {
            receivedType = pinType;
            return Task.FromResult(PinSubmissionResult.Success(pinType));
        }, "PinPad");

        var intent = new IntentResult
        {
            Intent = InputIntent.PinCompleted,
            Confidence = 1.0,
            RawInput = "4829",
            Context = InputContext.PinEntry,
            ResolvedValue = null // no resolved value
        };

        await RaiseIntent(intent);

        Assert.Equal("UserPin", receivedType);
    }

    [Fact]
    public async Task SixDigitPinWithoutResolvedValue_InfersMasterPin()
    {
        string? receivedType = null;
        _sut.RegisterHandler((_, pinType) =>
        {
            receivedType = pinType;
            return Task.FromResult(PinSubmissionResult.Success(pinType));
        }, "PinPad");

        var intent = new IntentResult
        {
            Intent = InputIntent.PinCompleted,
            Confidence = 1.0,
            RawInput = "829417",
            Context = InputContext.PinEntry,
            ResolvedValue = null
        };

        await RaiseIntent(intent);

        Assert.Equal("MasterPin", receivedType);
    }

    // ═══════════════════════════════════════════════════════════════
    // Handler receives correct PIN value
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Handler_ReceivesExactPinValue()
    {
        string? receivedPin = null;
        _sut.RegisterHandler((pin, pinType) =>
        {
            receivedPin = pin;
            return Task.FromResult(PinSubmissionResult.Success(pinType));
        }, "PinPad");

        await RaiseIntent(MakeUserPin("9753"));

        Assert.Equal("9753", receivedPin);
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
    public void Dispose_ClearsHandler()
    {
        _sut.RegisterHandler(SuccessHandler(), "PinPad");
        _sut.Dispose();

        Assert.False(_sut.IsHandlerRegistered);
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
    // PinSubmissionResult record
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void PinSubmissionResult_Success_Properties()
    {
        var r = PinSubmissionResult.Success("UserPin");
        Assert.True(r.Succeeded);
        Assert.Empty(r.ErrorMessage);
        Assert.Equal("UserPin", r.PinType);
    }

    [Fact]
    public void PinSubmissionResult_Failure_Properties()
    {
        var r = PinSubmissionResult.Failure("MasterPin", "Wrong PIN");
        Assert.False(r.Succeeded);
        Assert.Equal("Wrong PIN", r.ErrorMessage);
        Assert.Equal("MasterPin", r.PinType);
    }

    [Fact]
    public void PinSubmissionResult_RecordEquality()
    {
        var a = PinSubmissionResult.Success("UserPin");
        var b = PinSubmissionResult.Success("UserPin");
        Assert.Equal(a, b);
    }

    // ═══════════════════════════════════════════════════════════════
    // Event records
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void PinAutoSubmittedEvent_RecordEquality()
    {
        var a = new PinAutoSubmittedEvent("UserPin");
        var b = new PinAutoSubmittedEvent("UserPin");
        Assert.Equal(a, b);
    }

    [Fact]
    public void PinSubmissionFailedEvent_RecordEquality()
    {
        var a = new PinSubmissionFailedEvent("UserPin", "Wrong");
        var b = new PinSubmissionFailedEvent("UserPin", "Wrong");
        Assert.Equal(a, b);
    }
}
