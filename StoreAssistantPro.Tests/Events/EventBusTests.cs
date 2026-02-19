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
}
