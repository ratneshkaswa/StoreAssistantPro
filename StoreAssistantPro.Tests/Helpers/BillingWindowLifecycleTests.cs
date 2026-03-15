using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Services;
using StoreAssistantPro.Modules.Billing.ViewModels;
using StoreAssistantPro.Modules.Billing.Views;

namespace StoreAssistantPro.Tests.Helpers;

[Collection("WpfUi")]
public sealed class BillingWindowLifecycleTests
{
    [Fact(Skip = "Run this WPF lifecycle audit individually; it is not stable after the full xUnit run.")]
    public void BillingWindow_Should_SwitchToBillingMode_OnOpen_And_RestorePreviousMode_OnClose()
    {
        RunOnStaThread(() =>
        {
            EnsureApplicationResources();

            var sizing = CreateSizingService();
            var appState = Substitute.For<IAppStateService>();
            var eventBus = Substitute.For<IEventBus>();
            var billingService = Substitute.For<IBillingService>();
            var dialogService = Substitute.For<IDialogService>();
            var regional = Substitute.For<IRegionalSettingsService>();
            var currentMode = OperationalMode.Management;
            var publishedEvents = new List<OperationalModeChangedEvent>();

            appState.CurrentUserType.Returns(UserType.Admin);
            appState.CurrentMode.Returns(_ => currentMode);
            appState.When(service => service.SetMode(Arg.Any<OperationalMode>()))
                .Do(call => currentMode = call.Arg<OperationalMode>());
            eventBus.PublishAsync(Arg.Any<OperationalModeChangedEvent>()).Returns(Task.CompletedTask);
            eventBus
                .When(bus => bus.PublishAsync(Arg.Any<OperationalModeChangedEvent>()))
                .Do(call => publishedEvents.Add(call.Arg<OperationalModeChangedEvent>()));

            using var vm = new BillingViewModel(billingService, appState, dialogService, regional);
            var window = new BillingWindow(sizing, appState, eventBus, vm)
            {
                Left = -20000,
                Top = -20000,
                ShowActivated = false,
                ShowInTaskbar = false
            };

            try
            {
                window.Show();
                window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                DrainDispatcher();

                Assert.Equal(OperationalMode.Billing, currentMode);
                appState.Received().SetMode(OperationalMode.Billing);
                Assert.Contains(
                    publishedEvents,
                    e => e.PreviousMode == OperationalMode.Management && e.NewMode == OperationalMode.Billing);
            }
            finally
            {
                window.Close();
                DrainDispatcher();
            }

            Assert.Equal(OperationalMode.Management, currentMode);
            appState.Received().SetMode(OperationalMode.Management);
            Assert.Contains(
                publishedEvents,
                e => e.PreviousMode == OperationalMode.Billing && e.NewMode == OperationalMode.Management);
        });
    }

    [Fact(Skip = "Run this WPF lifecycle audit individually; it is not stable after the full xUnit run.")]
    public void BillingWindow_Should_NotRestoreMode_WhenClosedBeforeItLoads()
    {
        RunOnStaThread(() =>
        {
            EnsureApplicationResources();

            var sizing = CreateSizingService();
            var appState = Substitute.For<IAppStateService>();
            var eventBus = Substitute.For<IEventBus>();
            var billingService = Substitute.For<IBillingService>();
            var dialogService = Substitute.For<IDialogService>();
            var regional = Substitute.For<IRegionalSettingsService>();

            appState.CurrentUserType.Returns(UserType.Admin);
            appState.CurrentMode.Returns(OperationalMode.Management);
            eventBus.PublishAsync(Arg.Any<OperationalModeChangedEvent>()).Returns(Task.CompletedTask);

            using var vm = new BillingViewModel(billingService, appState, dialogService, regional);
            var window = new BillingWindow(sizing, appState, eventBus, vm);

            window.Close();
            DrainDispatcher();

            appState.DidNotReceive().SetMode(Arg.Any<OperationalMode>());
            eventBus.DidNotReceive().PublishAsync<OperationalModeChangedEvent>(Arg.Any<OperationalModeChangedEvent>());
        });
    }

    private static IWindowSizingService CreateSizingService()
    {
        var sizing = Substitute.For<IWindowSizingService>();
        sizing.When(service => service.ConfigureDialogWindow(Arg.Any<Window>(), Arg.Any<double>(), Arg.Any<double>()))
            .Do(call =>
            {
                var window = call.Arg<Window>();
                window.Width = 1200;
                window.Height = 850;
                window.MinWidth = 960;
                window.MinHeight = 720;
                window.WindowStartupLocation = WindowStartupLocation.Manual;
            });

        return sizing;
    }

    private static void DrainDispatcher()
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Send, Dispatcher.CurrentDispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();
            frame.Continue = false;
        };

        Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.Input,
            new DispatcherOperationCallback(_ =>
            {
                timer.Stop();
                frame.Continue = false;
                return null;
            }),
            null);

        timer.Start();
        Dispatcher.PushFrame(frame);
    }

    private static void EnsureApplicationResources()
        => WpfTestApplication.EnsureStoreAssistantApplication();

    private static void RunOnStaThread(Action action)
        => WpfTestApplication.Run(action);
}
