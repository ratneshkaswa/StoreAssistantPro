using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Authentication.ViewModels;
using StoreAssistantPro.Modules.MainShell.ViewModels;
using StoreAssistantPro.Modules.MainShell.Views;

namespace StoreAssistantPro.Tests.Helpers;

[Collection("WpfUi")]
public sealed class MainShellWorkflowSmokeTests
{
    private static readonly (string CommandProperty, Type WindowType)[] DialogCommands =
    [
    ];

    [Fact(Skip = "Run this real shell workflow smoke test individually; it is not stable after the full xUnit run.")]
    public void MainShellDialogCommands_Should_OpenRegisteredDialogs_ThroughRealWorkflowPath()
    {
        var failures = RunOnStaThread(() =>
        {
            EnsureApplicationResources();

            using var host = BuildHost();
            var failures = new List<string>();
            Window? keepAliveWindow = null;
            MainWindow? mainWindow = null;

            try
            {
                keepAliveWindow = CreateKeepAliveWindow();
                mainWindow = host.Services.GetRequiredService<MainWindow>();
                var viewModel = mainWindow.DataContext as MainViewModel
                    ?? host.Services.GetRequiredService<MainViewModel>();
                mainWindow.DataContext = viewModel;

                PrepareWindowForAudit(mainWindow);
                mainWindow.Show();
                DrainDispatcher();
                mainWindow.UpdateLayout();

                foreach (var (commandProperty, expectedWindowType) in DialogCommands)
                    AuditDialogCommand(mainWindow, viewModel, commandProperty, expectedWindowType, failures);
            }
            finally
            {
                CloseWindow(mainWindow);
                CloseWindow(keepAliveWindow);
                BaseViewModel.SetLoggerFactory(NullLoggerFactory.Instance);
            }

            return failures;
        });

        Assert.True(
            failures.Count == 0,
            $"Main shell workflow smoke audit failed:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    private static void AuditDialogCommand(
        MainWindow mainWindow,
        MainViewModel viewModel,
        string commandProperty,
        Type expectedWindowType,
        List<string> failures)
    {
        try
        {
            if (typeof(MainViewModel).GetProperty(commandProperty, BindingFlags.Instance | BindingFlags.Public)?.GetValue(viewModel) is not ICommand command)
            {
                failures.Add($"{commandProperty}: command property resolved to null.");
                return;
            }

            if (!command.CanExecute(null))
            {
                failures.Add($"{commandProperty}: command reported CanExecute(false).");
                return;
            }

            var openedWindowType = ExecuteModalCommandAndCaptureWindowType(command);
            if (openedWindowType is null)
            {
                failures.Add($"{commandProperty}: command returned without opening a dialog window.");
                return;
            }

            if (openedWindowType != expectedWindowType)
            {
                failures.Add(
                    $"{commandProperty}: opened {openedWindowType.Name} instead of expected {expectedWindowType.Name}.");
            }
        }
        catch (Exception ex)
        {
            failures.Add($"{commandProperty}: {ex.GetType().Name} - {ex.Message}");
        }
    }

    private static Type? ExecuteModalCommandAndCaptureWindowType(ICommand command)
    {
        Type? openedWindowType = null;
        var baselineWindows = (Application.Current?.Windows.OfType<Window>() ?? [])
            .ToHashSet();
        var completion = new DispatcherFrame();
        var timeout = new DispatcherTimer(DispatcherPriority.Send, Dispatcher.CurrentDispatcher)
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        var watcher = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(25)
        };

        timeout.Tick += (_, _) =>
        {
            timeout.Stop();
            watcher.Stop();
            completion.Continue = false;
            throw new TimeoutException("Timed out waiting for modal workflow command to open and close a dialog.");
        };

        watcher.Tick += (_, _) =>
        {
            var dialog = Application.Current?
                .Windows
                .OfType<Window>()
                .FirstOrDefault(window => window.IsVisible && !baselineWindows.Contains(window));

            if (dialog is null)
                return;

            openedWindowType ??= dialog.GetType();
            watcher.Stop();
            timeout.Stop();

            try
            {
                dialog.DialogResult = false;
            }
            catch (InvalidOperationException)
            {
                dialog.Close();
            }

            completion.Continue = false;
        };

        timeout.Start();
        watcher.Start();
        command.Execute(null);
        Dispatcher.PushFrame(completion);
        DrainDispatcher();
        return openedWindowType;
    }

    private static IHost BuildHost()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = FindContentRoot()
        });

        InvokeHostingExtension("AddDataAccess", builder.Services, builder.Configuration);
        InvokeHostingExtension("AddCoreServices", builder.Services);
        InvokeHostingExtension("AddModules", builder.Services);

        var host = builder.Build();
        host.Services
            .GetRequiredService<NavigationPageRegistry>()
            .ApplyTo(host.Services.GetRequiredService<INavigationService>());
        InvokeHostingExtension("ApplyDialogRegistrations", host.Services);
        BaseViewModel.SetLoggerFactory(host.Services.GetRequiredService<ILoggerFactory>());
        return host;
    }

    private static object? InvokeHostingExtension(string methodName, params object[] arguments)
    {
        var hostingExtensions = typeof(App).Assembly.GetType("StoreAssistantPro.HostingExtensions")
            ?? throw new InvalidOperationException("Could not locate HostingExtensions.");

        var method = hostingExtensions
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .SingleOrDefault(candidate =>
                candidate.Name == methodName
                && candidate.GetParameters().Length == arguments.Length)
            ?? throw new InvalidOperationException($"Could not locate HostingExtensions.{methodName}.");

        return method.Invoke(null, arguments);
    }

    private static string FindContentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "StoreAssistantPro.slnx"))
                && File.Exists(Path.Combine(directory.FullName, "appsettings.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the StoreAssistantPro content root.");
    }

    private static void EnsureApplicationResources()
        => WpfTestApplication.EnsureStoreAssistantApplication();

    private static Window CreateKeepAliveWindow()
    {
        var window = new Window
        {
            Width = 1,
            Height = 1,
            ShowActivated = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            WindowStyle = WindowStyle.None,
            Left = -20000,
            Top = -20000,
            Opacity = 0
        };

        window.Show();
        DrainDispatcher();
        return window;
    }

    private static void PrepareWindowForAudit(Window window)
    {
        window.ShowActivated = false;
        window.ShowInTaskbar = false;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -10000;
        window.Top = -10000;
    }

    private static void PrepareWindowForClose(Window window)
    {
        switch (window.DataContext)
        {
            case LoginViewModel loginViewModel:
                loginViewModel.IsVerifying = false;
                loginViewModel.IsBusy = false;
                break;
        }
    }

    private static void CloseWindow(Window? window)
    {
        if (window is null)
            return;

        PrepareWindowForClose(window);

        if (window.IsVisible)
            window.Close();

        DrainDispatcher();
    }

    private static void DrainDispatcher(DispatcherPriority priority = DispatcherPriority.Input)
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
            priority,
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

    private static T RunOnStaThread<T>(Func<T> action)
        => WpfTestApplication.Run(action);
}
