using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;
using StoreAssistantPro;

namespace StoreAssistantPro.Tests.Helpers;

/// <summary>
/// Manages WPF <see cref="Application"/> lifetime for runtime tests.
/// The App is created once and kept alive across all calls because WPF's internal
/// resource-package manager (<c>Application.GetResourcePackage</c>) holds static state
/// that cannot be reset — re-creating the App triggers <c>Environment.FailFast</c>.
/// Each <see cref="Run"/> call creates a fresh STA thread with its own dispatcher.
/// </summary>
internal static class WpfTestApplication
{
    private static readonly object SyncRoot = new();
    private static readonly FieldInfo AppInstanceField =
        typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Could not locate Application._appInstance.");
    private static readonly FieldInfo AppCreatedField =
        typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Could not locate Application._appCreatedInThisAppDomain.");
    private static readonly FieldInfo IsShuttingDownField =
        typeof(Application).GetField("_isShuttingDown", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Could not locate Application._isShuttingDown.");

    public static void EnsureStoreAssistantApplication()
    {
        if (Application.Current is not null)
            return;

        ResetApplicationState();

        // Create the real App once so runtime render tests get the same
        // resource graph, converters, and DataTemplates as production.
        // OnStartup does not run until Application.Run(), so DI boot does
        // not execute in these tests.
        var application = new App
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };
        application.InitializeComponent();
    }

    public static void Run(Action action)
        => Run(() =>
        {
            action();
            return true;
        });

    public static T Run<T>(Func<T> action)
    {
        T? result = default;
        Exception? exception = null;
        using var completed = new ManualResetEventSlim(false);

        var thread = new Thread(() =>
        {
            try
            {
                EnsureStoreAssistantApplication();
                result = action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                try
                {
                    CloseAllWindows();
                }
                catch
                {
                    // Best-effort test cleanup.
                }

                // Do NOT call Dispatcher.InvokeShutdown(). Shutting down
                // the dispatcher triggers Application.DoShutdown which
                // nulls _appInstance. The App must stay alive so subsequent
                // tests skip InitializeComponent (which FailFasts on re-entry).
                // The background STA thread exits naturally when the lambda
                // completes; the dispatcher is simply abandoned.

                completed.Set();
            }
        });

        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!completed.Wait(TimeSpan.FromSeconds(30)))
            throw new TimeoutException("The STA WPF test thread did not complete within 30 seconds.");

        if (exception is not null)
            ExceptionDispatchInfo.Capture(exception).Throw();

        return result!;
    }

    public static void ShutdownCurrentApplication()
    {
        CloseAllWindows();
        ResetApplicationState();
    }

    public static void FlushDispatcher()
    {
        Dispatcher.CurrentDispatcher.Invoke(
            DispatcherPriority.Background,
            new Action(() => { }));
    }

    public static void WaitForDispatcher(TimeSpan duration)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = duration
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();
            frame.Continue = false;
        };

        timer.Start();
        Dispatcher.PushFrame(frame);
    }

    private static void CloseAllWindows()
    {
        var current = Application.Current;
        if (current is null)
            return;

        foreach (var window in current.Windows.OfType<Window>().ToArray())
        {
            try
            {
                window.Close();
            }
            catch
            {
                // Best-effort test cleanup.
            }
        }
    }

    private static void ResetApplicationState()
    {
        lock (SyncRoot)
        {
            AppInstanceField.SetValue(null, null);
            AppCreatedField.SetValue(null, false);
            IsShuttingDownField.SetValue(null, false);
        }
    }
}
