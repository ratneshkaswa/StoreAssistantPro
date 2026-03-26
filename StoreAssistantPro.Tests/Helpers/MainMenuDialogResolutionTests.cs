using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Authentication.ViewModels;
using StoreAssistantPro.Modules.MainShell.ViewModels;
using StoreAssistantPro.Modules.MainShell.Views;

namespace StoreAssistantPro.Tests.Helpers;

[Collection("WpfUi")]
public class MainMenuDialogResolutionTests
{
    private static readonly Type[] StartupWindowTypes =
    [
    ];

    private static readonly Type[] DialogWindowTypes =
    [
    ];

    [Fact(Skip = "Run this host-bootstrapped WPF wiring audit individually; it is not stable after the full xUnit run.")]
    public void ApplicationWindows_ShouldResolve_AndLoadInteractiveWiring()
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

                foreach (var windowType in StartupWindowTypes)
                    AuditResolvedWindow(host.Services, windowType, failures);

                AuditResolvedWindow(host.Services, typeof(MainWindow), failures);
                ClearDialogOwner(host.Services);

                foreach (var windowType in DialogWindowTypes)
                    AuditResolvedWindow(host.Services, windowType, failures);
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
            $"Application window audit failed:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    private static void AuditResolvedWindow(IServiceProvider services, Type windowType, List<string> failures)
    {
        try
        {
            var window = ResolveWindow(services, windowType);
            AuditWindow(window, failures);
        }
        catch (Exception ex)
        {
            failures.Add($"{windowType.Name}: {ex.GetType().Name} - {ex.Message}");
        }
    }

    private static Window ResolveWindow(IServiceProvider services, Type windowType)
    {
        var window = (Window)services.GetRequiredService(windowType);

        if (window is MainWindow mainWindow && mainWindow.DataContext is null)
            mainWindow.DataContext = services.GetRequiredService<MainViewModel>();

        return window;
    }

    private static void AuditWindow(Window window, List<string> failures, bool closeWhenDone = true)
    {
        var openedPopups = new List<Popup>();

        try
        {
            PrepareWindowForAudit(window);

            window.ApplyTemplate();
            _ = window.Content;
            _ = window.DataContext;

            window.Show();
            DrainDispatcher();
            window.UpdateLayout();

            openedPopups.AddRange(OpenPopups(window));
            DrainDispatcher();
            window.UpdateLayout();

            foreach (var element in EnumerateTree(window))
            {
                AuditAttachedCommandBinding(
                    window.GetType().Name,
                    element,
                    BaseDialogWindow.ConfirmCommandProperty,
                    BaseDialogWindow.ConfirmCommandParameterProperty,
                    "ConfirmCommand",
                    failures);

                AuditAttachedCommandBinding(
                    window.GetType().Name,
                    element,
                    KeyboardNav.DefaultCommandProperty,
                    KeyboardNav.DefaultCommandParameterProperty,
                    "KeyboardNav.DefaultCommand",
                    failures);

                AuditAttachedCommandBinding(
                    window.GetType().Name,
                    element,
                    KeyboardNav.EscapeCommandProperty,
                    KeyboardNav.EscapeCommandParameterProperty,
                    "KeyboardNav.EscapeCommand",
                    failures);

                switch (element)
                {
                    case ButtonBase button:
                        AuditCommandBinding(
                            window.GetType().Name,
                            button,
                            ButtonBase.CommandProperty,
                            ButtonBase.CommandParameterProperty,
                            failures);
                        break;
                    case MenuItem menuItem:
                        AuditCommandBinding(
                            window.GetType().Name,
                            menuItem,
                            MenuItem.CommandProperty,
                            MenuItem.CommandParameterProperty,
                            failures);
                        break;
                    case Hyperlink hyperlink:
                        AuditCommandBinding(
                            window.GetType().Name,
                            hyperlink,
                            Hyperlink.CommandProperty,
                            Hyperlink.CommandParameterProperty,
                            failures);
                        break;
                }

                AuditInputBindings(window.GetType().Name, element, failures);
            }
        }
        finally
        {
            foreach (var popup in openedPopups)
                popup.IsOpen = false;

            DrainDispatcher();

            if (closeWhenDone)
                CloseWindow(window);
        }
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
                loginViewModel.IsBusy = false;
                loginViewModel.IsLoading = false;
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

    private static void ClearDialogOwner(IServiceProvider services)
    {
        var sizingService = services.GetRequiredService<IWindowSizingService>();
        var field = sizingService.GetType().GetField("_mainWindow", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate WindowSizingService._mainWindow.");

        field.SetValue(sizingService, null);
    }

    private static IEnumerable<Popup> OpenPopups(Window window)
    {
        var popups = EnumerateTree(window).OfType<Popup>().ToList();

        foreach (var popup in popups)
            popup.IsOpen = true;

        return popups;
    }

    private static void AuditCommandBinding(
        string windowName,
        DependencyObject source,
        DependencyProperty commandProperty,
        DependencyProperty parameterProperty,
        List<string> failures)
    {
        if (BindingOperations.GetBindingExpressionBase(source, commandProperty) is null)
            return;

        if (source.GetValue(commandProperty) is not ICommand command)
        {
            failures.Add($"{windowName}: {DescribeSource(source)} command binding resolved to null.");
            return;
        }

        var parameter = source.GetValue(parameterProperty);

        try
        {
            _ = command.CanExecute(parameter);
        }
        catch (Exception ex)
        {
            failures.Add(
                $"{windowName}: {DescribeSource(source)} command CanExecute threw {ex.GetType().Name} - {ex.Message}");
        }
    }

    private static void AuditAttachedCommandBinding(
        string windowName,
        DependencyObject source,
        DependencyProperty commandProperty,
        DependencyProperty parameterProperty,
        string bindingName,
        List<string> failures)
    {
        if (BindingOperations.GetBindingExpressionBase(source, commandProperty) is null)
            return;

        if (source.GetValue(commandProperty) is not ICommand command)
        {
            failures.Add($"{windowName}: {DescribeSource(source)} {bindingName} binding resolved to null.");
            return;
        }

        var parameter = source.GetValue(parameterProperty);

        try
        {
            _ = command.CanExecute(parameter);
        }
        catch (Exception ex)
        {
            failures.Add(
                $"{windowName}: {DescribeSource(source)} {bindingName} CanExecute threw {ex.GetType().Name} - {ex.Message}");
        }
    }

    private static void AuditInputBindings(string windowName, DependencyObject source, List<string> failures)
    {
        IEnumerable<InputBinding> inputBindings = source switch
        {
            UIElement element => element.InputBindings.Cast<InputBinding>(),
            ContentElement element => element.InputBindings.Cast<InputBinding>(),
            _ => []
        };

        foreach (var inputBinding in inputBindings)
        {
            if (BindingOperations.GetBindingExpressionBase(inputBinding, InputBinding.CommandProperty) is null)
                continue;

            if (inputBinding.Command is null)
            {
                failures.Add(
                    $"{windowName}: {DescribeSource(source)} input binding ({inputBinding.Gesture}) resolved to null.");
                continue;
            }

            try
            {
                _ = inputBinding.Command.CanExecute(inputBinding.CommandParameter);
            }
            catch (Exception ex)
            {
                failures.Add(
                    $"{windowName}: {DescribeSource(source)} input binding ({inputBinding.Gesture}) CanExecute threw {ex.GetType().Name} - {ex.Message}");
            }
        }
    }

    private static string DescribeSource(DependencyObject source)
    {
        var typeName = source.GetType().Name;

        var name = source switch
        {
            FrameworkElement element when !string.IsNullOrWhiteSpace(element.Name) => $"#{element.Name}",
            FrameworkContentElement element when !string.IsNullOrWhiteSpace(element.Name) => $"#{element.Name}",
            _ => string.Empty
        };

        var label = source switch
        {
            ButtonBase button when button.Content is string text && !string.IsNullOrWhiteSpace(text) => $"[{text}]",
            MenuItem menuItem when menuItem.Header is string text && !string.IsNullOrWhiteSpace(text) => $"[{text}]",
            Hyperlink hyperlink => GetHyperlinkText(hyperlink),
            _ => string.Empty
        };

        return $"{typeName}{name}{label}";
    }

    private static string GetHyperlinkText(Hyperlink hyperlink)
    {
        var text = string.Concat(hyperlink.Inlines.OfType<Run>().Select(run => run.Text)).Trim();
        return string.IsNullOrWhiteSpace(text) ? string.Empty : $"[{text}]";
    }

    private static IEnumerable<DependencyObject> EnumerateTree(DependencyObject root)
    {
        var stack = new Stack<DependencyObject>();
        var visited = new HashSet<DependencyObject>(ReferenceEqualityComparer.Instance);
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
                continue;

            yield return current;

            if (current is Popup popup && popup.Child is not null)
                stack.Push(popup.Child);

            foreach (var child in LogicalTreeHelper.GetChildren(current).OfType<DependencyObject>())
                stack.Push(child);

            if (current is Visual or Visual3D)
            {
                for (var i = VisualTreeHelper.GetChildrenCount(current) - 1; i >= 0; i--)
                    stack.Push(VisualTreeHelper.GetChild(current, i));
            }
        }
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

    private static T RunOnStaThread<T>(Func<T> action)
        => WpfTestApplication.Run(action);
}
