using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;
using StoreAssistantPro.Core.Controls;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class TipBannerAutoStateTests
{
    [Fact]
    public void ContextRefresh_Should_Not_Append_UsageTip_Repeatedly()
    {
        RunOnStaThread(() =>
        {
            var previousResolver = TipBannerAutoState.ContextResolver;
            TipBannerAutoState.ContextResolver = _ => new ContextHelpResult(
                Description: null,
                UsageTip: "Keep the scanner ready.",
                Suffix: null);

            var banner = new InlineTipBanner
            {
                Title = "Quick tip",
                TipText = "Base tip"
            };

            TipBannerAutoState.SetContextKey(banner, "Billing");

            try
            {
                banner.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                DrainDispatcher();

                Assert.Equal("Base tip Keep the scanner ready.", banner.TipText);

                TipBannerAutoState.OnContextChanged();
                DrainDispatcher();
                Assert.Equal("Base tip Keep the scanner ready.", banner.TipText);

                TipBannerAutoState.OnContextChanged();
                DrainDispatcher();
                Assert.Equal("Base tip Keep the scanner ready.", banner.TipText);
            }
            finally
            {
                TipBannerAutoState.ContextResolver = previousResolver;
                banner.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
                DrainDispatcher();
            }
        });
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

    private static void RunOnStaThread(Action action)
        => WpfTestApplication.Run(action);
}
