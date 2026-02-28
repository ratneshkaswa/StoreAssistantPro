using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Tests.Events;

public class EventBusTests
{
    private sealed record TestEvent(string Value) : IEvent;
    private sealed record OtherEvent(int Number) : IEvent;

    [Fact]
    public async Task PublishAsync_NotifiesSubscriber()
    {
        var bus = new EventBus();
        string? received = null;

        bus.Subscribe<TestEvent>(e =>
        {
            received = e.Value;
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestEvent("hello"));

        Assert.Equal("hello", received);
    }

    [Fact]
    public async Task PublishAsync_NotifiesMultipleSubscribers()
    {
        var bus = new EventBus();
        var calls = new List<string>();

        bus.Subscribe<TestEvent>(e => { calls.Add("A:" + e.Value); return Task.CompletedTask; });
        bus.Subscribe<TestEvent>(e => { calls.Add("B:" + e.Value); return Task.CompletedTask; });

        await bus.PublishAsync(new TestEvent("x"));

        Assert.Equal(["A:x", "B:x"], calls);
    }

    [Fact]
    public async Task PublishAsync_DoesNotNotifyUnrelatedSubscribers()
    {
        var bus = new EventBus();
        var testCalled = false;

        bus.Subscribe<OtherEvent>(_ => { testCalled = true; return Task.CompletedTask; });

        await bus.PublishAsync(new TestEvent("x"));

        Assert.False(testCalled);
    }

    [Fact]
    public async Task Unsubscribe_RemovesHandler()
    {
        var bus = new EventBus();
        var callCount = 0;

        Task Handler(TestEvent _) { callCount++; return Task.CompletedTask; }

        bus.Subscribe<TestEvent>(Handler);
        await bus.PublishAsync(new TestEvent("1"));
        Assert.Equal(1, callCount);

        bus.Unsubscribe<TestEvent>(Handler);
        await bus.PublishAsync(new TestEvent("2"));
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task PublishAsync_NoSubscribers_DoesNotThrow()
    {
        var bus = new EventBus();

        var ex = await Record.ExceptionAsync(() => bus.PublishAsync(new TestEvent("x")));

        Assert.Null(ex);
    }

    [Fact]
    public async Task PublishAsync_ExecutesHandlersSequentially()
    {
        var bus = new EventBus();
        var order = new List<int>();

        bus.Subscribe<TestEvent>(async _ =>
        {
            await Task.Delay(10);
            order.Add(1);
        });
        bus.Subscribe<TestEvent>(_ =>
        {
            order.Add(2);
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestEvent("x"));

        Assert.Equal([1, 2], order);
    }

    // ── Fault isolation ──

    [Fact]
    public async Task PublishAsync_ThrowingHandler_DoesNotBlockOtherSubscribers()
    {
        var bus = new EventBus();
        var secondCalled = false;

        bus.Subscribe<TestEvent>(_ => throw new InvalidOperationException("boom"));
        bus.Subscribe<TestEvent>(_ => { secondCalled = true; return Task.CompletedTask; });

        await bus.PublishAsync(new TestEvent("x"));

        Assert.True(secondCalled);
    }

    // ── Weak reference behavior ──

    [Fact]
    public async Task PublishAsync_InstanceMethodHandler_NotCalledAfterTargetCollected()
    {
        var bus = new EventBus();
        var callCount = 0;

        SubscribeFromShortLivedObject(bus, () => callCount++);

        // The subscriber object is now out of scope.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        await bus.PublishAsync(new TestEvent("after-gc"));

        Assert.Equal(0, callCount);
    }

    [Fact]
    public async Task PublishAsync_InstanceMethodHandler_CalledWhileTargetAlive()
    {
        var bus = new EventBus();
        var callCount = 0;

        var subscriber = new TestSubscriber(() => callCount++);
        bus.Subscribe<TestEvent>(subscriber.HandleAsync);

        await bus.PublishAsync(new TestEvent("alive"));

        Assert.Equal(1, callCount);
        GC.KeepAlive(subscriber);
    }

    [Fact]
    public async Task Unsubscribe_InstanceMethodHandler_AfterTargetCollected_NoException()
    {
        var bus = new EventBus();
        Func<TestEvent, Task>? handler = null;

        SubscribeAndCaptureHandler(bus, ref handler);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Unsubscribe after target is collected — should not throw.
        var ex = Record.Exception(() => bus.Unsubscribe(handler!));
        Assert.Null(ex);

        // Publish should also not throw.
        await bus.PublishAsync(new TestEvent("x"));
    }

    // ── Helpers ──

    /// <summary>
    /// Subscribes an instance-method handler from a short-lived object
    /// that goes out of scope immediately, letting the GC collect it.
    /// </summary>
    private static void SubscribeFromShortLivedObject(EventBus bus, Action onCalled)
    {
        var subscriber = new TestSubscriber(onCalled);
        bus.Subscribe<TestEvent>(subscriber.HandleAsync);
        // subscriber goes out of scope after this method returns.
    }

    private static void SubscribeAndCaptureHandler(EventBus bus, ref Func<TestEvent, Task>? handler)
    {
        var subscriber = new TestSubscriber(() => { });
        handler = subscriber.HandleAsync;
        bus.Subscribe<TestEvent>(handler);
        // subscriber goes out of scope after this method returns.
    }

    /// <summary>
    /// A disposable subscriber whose instance method can be used as an
    /// event handler. When the GC collects this object, the EventBus
    /// should stop invoking its handler.
    /// </summary>
    private sealed class TestSubscriber(Action onCalled)
    {
        public Task HandleAsync(TestEvent e)
        {
            onCalled();
            return Task.CompletedTask;
        }
    }
}
