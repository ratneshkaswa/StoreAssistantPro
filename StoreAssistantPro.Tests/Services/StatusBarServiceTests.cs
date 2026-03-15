using System.Runtime.ExceptionServices;
using System.Windows.Threading;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Tests.Services;

public sealed class StatusBarServiceTests
{
    [Fact]
    public void SetPersistent_FromBackgroundThread_Should_MarshalToDispatcher()
    {
        RunOnStaThread(() =>
        {
            var sut = new StatusBarService(Dispatcher.CurrentDispatcher);

            var worker = new Thread(() => sut.SetPersistent("Offline mode active"));
            worker.Start();
            WaitForThread(worker);

            Assert.Equal("Offline mode active", sut.Message);
        });
    }

    [Fact]
    public void Post_FromBackgroundThread_Should_MarshalToDispatcher()
    {
        RunOnStaThread(() =>
        {
            var sut = new StatusBarService(Dispatcher.CurrentDispatcher);

            var worker = new Thread(() => sut.Post("Connection restored", TimeSpan.FromMinutes(1)));
            worker.Start();
            WaitForThread(worker);

            Assert.Equal("Connection restored", sut.Message);
        });
    }

    [Fact]
    public void Dispose_Should_StopClearTimer_And_BeIdempotent()
    {
        RunOnStaThread(() =>
        {
            var sut = new StatusBarService(Dispatcher.CurrentDispatcher);
            sut.Post("Connection restored", TimeSpan.FromMinutes(1));

            var timer = GetPrivateClearTimer(sut);
            Assert.True(timer.IsEnabled);

            sut.Dispose();
            sut.Dispose();

            Assert.False(timer.IsEnabled);
        });
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? failure = null;
        using var completed = new ManualResetEventSlim(false);

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
                completed.Set();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.True(completed.Wait(TimeSpan.FromSeconds(10)), "STA test thread timed out.");

        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static void WaitForThread(Thread thread)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (thread.IsAlive && DateTime.UtcNow < deadline)
            DrainDispatcher();

        Assert.False(thread.IsAlive, "Worker thread timed out.");
    }

    private static void DrainDispatcher()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new DispatcherOperationCallback(_ =>
            {
                frame.Continue = false;
                return null;
            }),
            null);
        Dispatcher.PushFrame(frame);
    }

    private static DispatcherTimer GetPrivateClearTimer(StatusBarService sut) =>
        (DispatcherTimer)(typeof(StatusBarService)
            .GetField("_clearTimer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(sut)
            ?? throw new InvalidOperationException("Could not read StatusBarService._clearTimer."));
}
