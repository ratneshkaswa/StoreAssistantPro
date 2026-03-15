using System.Runtime.ExceptionServices;
using System.Windows.Threading;
using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Services;

public sealed class AppStateServiceTests
{
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();

    private AppStateService CreateSut(Dispatcher? dispatcher = null)
    {
        _regional.Now.Returns(new DateTime(2026, 3, 15, 10, 30, 0));
        _regional.FormatTime(Arg.Any<DateTime>()).Returns("10:30 AM");
        return new AppStateService(_regional, dispatcher);
    }

    [Fact]
    public void UnreadNotificationCount_ShouldTrack_DirectIsReadChanges()
    {
        var sut = CreateSut();
        var notification = new AppNotification
        {
            Title = "Inventory sync warning",
            Message = "Synchronization is waiting for connectivity.",
            Timestamp = new DateTime(2026, 3, 15, 10, 30, 0)
        };

        sut.AddNotification(notification);
        Assert.Equal(1, sut.UnreadNotificationCount);

        notification.IsRead = true;
        Assert.Equal(0, sut.UnreadNotificationCount);

        notification.IsRead = false;
        Assert.Equal(1, sut.UnreadNotificationCount);
    }

    [Fact]
    public void UnreadNotificationCount_ShouldIgnore_RemovedNotificationChanges()
    {
        var sut = CreateSut();
        var notification = new AppNotification
        {
            Title = "Low stock",
            Message = "A removed notification should no longer affect the badge.",
            Timestamp = new DateTime(2026, 3, 15, 10, 31, 0)
        };

        sut.AddNotification(notification);
        Assert.Equal(1, sut.UnreadNotificationCount);

        sut.Notifications.Remove(notification);
        Assert.Equal(0, sut.UnreadNotificationCount);

        notification.IsRead = true;
        notification.IsRead = false;
        Assert.Equal(0, sut.UnreadNotificationCount);
    }

    [Fact]
    public void UnreadNotificationCount_ShouldTrack_ReplaceTransitions()
    {
        var sut = CreateSut();
        var original = new AppNotification
        {
            Title = "Old",
            Message = "This read notification will be replaced.",
            Timestamp = new DateTime(2026, 3, 15, 10, 32, 0),
            IsRead = true
        };
        var replacement = new AppNotification
        {
            Title = "New",
            Message = "Replacement remains unread.",
            Timestamp = new DateTime(2026, 3, 15, 10, 33, 0)
        };

        sut.AddNotification(original);
        Assert.Equal(0, sut.UnreadNotificationCount);

        sut.Notifications[0] = replacement;
        Assert.Equal(1, sut.UnreadNotificationCount);

        original.IsRead = false;
        Assert.Equal(1, sut.UnreadNotificationCount);
    }

    [Fact]
    public void SetConnectivity_FromBackgroundThread_Should_MarshalToDispatcher()
    {
        RunOnStaThread(() =>
        {
            var sut = CreateSut(Dispatcher.CurrentDispatcher);
            var expected = new DateTime(2026, 3, 15, 10, 45, 0);

            var worker = new Thread(() => sut.SetConnectivity(true, expected));
            worker.Start();
            WaitForThread(worker);

            Assert.True(sut.IsOfflineMode);
            Assert.Equal(expected, sut.LastConnectionCheck);
        });
    }

    [Fact]
    public void AddNotification_FromBackgroundThread_Should_MarshalToDispatcher()
    {
        RunOnStaThread(() =>
        {
            var sut = CreateSut(Dispatcher.CurrentDispatcher);
            var notification = new AppNotification
            {
                Title = "Offline",
                Message = "Connectivity changed on a worker thread.",
                Timestamp = new DateTime(2026, 3, 15, 10, 46, 0)
            };

            var worker = new Thread(() => sut.AddNotification(notification));
            worker.Start();
            WaitForThread(worker);

            Assert.Single(sut.Notifications);
            Assert.Same(notification, sut.Notifications[0]);
            Assert.Equal(1, sut.UnreadNotificationCount);
        });
    }

    [Fact]
    public void Dispose_Should_StopClockTimer_And_UntrackNotifications()
    {
        RunOnStaThread(() =>
        {
            var sut = CreateSut(Dispatcher.CurrentDispatcher);
            var notification = new AppNotification
            {
                Title = "Lifecycle",
                Message = "Disposal should detach notification listeners.",
                Timestamp = new DateTime(2026, 3, 15, 10, 47, 0)
            };

            sut.SetLoggedIn(true);
            sut.AddNotification(notification);

            var timer = GetPrivateClockTimer(sut);
            Assert.True(timer.IsEnabled);
            Assert.Equal(1, sut.UnreadNotificationCount);

            sut.Dispose();
            notification.IsRead = true;

            Assert.False(timer.IsEnabled);
            Assert.Equal(1, sut.UnreadNotificationCount);
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

    private static DispatcherTimer GetPrivateClockTimer(AppStateService sut) =>
        (DispatcherTimer)(typeof(AppStateService)
            .GetField("_clockTimer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(sut)
            ?? throw new InvalidOperationException("Could not read AppStateService._clockTimer."));
}
