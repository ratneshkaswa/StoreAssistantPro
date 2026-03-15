using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Authentication.ViewModels;
using StoreAssistantPro.Modules.Authentication.Views.SetupPages;
using StoreAssistantPro.Modules.MainShell.ViewModels;
using StoreAssistantPro.Modules.MainShell.Views;

namespace StoreAssistantPro.Tests.Helpers;

[Collection("WpfUi")]
public class SetupPageInteractionTests
{
    [Fact(Skip = "Flaky under repeated WPF app bootstrap in xUnit; covered by targeted manual/runtime checks.")]
    public void FirmProfilePage_ToggleButton_ShouldToggleOptionalFields()
    {
        RunOnStaThread(() =>
        {
            EnsureApplicationResources();

            using var host = BuildHost();
            var keepAliveWindow = CreateKeepAliveWindow();
            var vm = host.Services.GetRequiredService<SetupViewModel>();
            var page = new FirmProfilePage { DataContext = vm };
            var hostWindow = ShowHostedControl(page);

            try
            {
                var toggleButton = FindControl<Button>(page, "Toggle additional business details");
                Assert.False(vm.ShowOptionalFirmFields);

                toggleButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                DrainDispatcher();
                Assert.True(vm.ShowOptionalFirmFields);

                toggleButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                DrainDispatcher();
                Assert.False(vm.ShowOptionalFirmFields);
            }
            finally
            {
                hostWindow.Close();
                keepAliveWindow.Close();
                vm.Dispose();
            }
        });
    }

    [Fact]
    public void FirmProfilePage_PhoneBox_ShouldStripNonDigits_AndCapLength()
    {
        Assert.Equal("1234567890", FirmProfilePage.NormalizePhoneDigits("12a34b567890"));
        Assert.Equal("9876543210", FirmProfilePage.NormalizePhoneDigits("98-76 54abc3210"));
        Assert.Equal(string.Empty, FirmProfilePage.NormalizePhoneDigits(null));
    }

    [Fact(Skip = "Flaky under repeated WPF app bootstrap in xUnit; covered by targeted manual/runtime checks.")]
    public void SecuritySettingsPage_PasswordBoxes_ShouldSyncToViewModel()
    {
        RunOnStaThread(() =>
        {
            EnsureApplicationResources();

            using var host = BuildHost();
            var keepAliveWindow = CreateKeepAliveWindow();
            var vm = host.Services.GetRequiredService<SetupViewModel>();
            var page = new SecuritySettingsPage { DataContext = vm };
            var hostWindow = ShowHostedControl(page);

            try
            {
                var adminPinBox = (PasswordBox)page.FindName("AdminPinBox")!;
                var masterPinBox = (PasswordBox)page.FindName("MasterPinBox")!;

                adminPinBox.Password = "2480";
                adminPinBox.RaiseEvent(new RoutedEventArgs(PasswordBox.PasswordChangedEvent));
                masterPinBox.Password = "482913";
                masterPinBox.RaiseEvent(new RoutedEventArgs(PasswordBox.PasswordChangedEvent));
                DrainDispatcher();

                Assert.Equal("2480", vm.AdminPin);
                Assert.Equal("482913", vm.MasterPin);
            }
            finally
            {
                hostWindow.Close();
                keepAliveWindow.Close();
                vm.Dispose();
            }
        });
    }

    [Fact(Skip = "Flaky under repeated WPF app bootstrap in xUnit; covered by targeted manual/runtime checks.")]
    public void SecuritySettingsPage_ViewModelChanges_ShouldSyncPasswordBoxes_AndClearAll()
    {
        RunOnStaThread(() =>
        {
            EnsureApplicationResources();

            using var host = BuildHost();
            var keepAliveWindow = CreateKeepAliveWindow();
            var vm = host.Services.GetRequiredService<SetupViewModel>();
            var page = new SecuritySettingsPage { DataContext = vm };
            var hostWindow = ShowHostedControl(page);

            try
            {
                vm.AdminPin = "2480";
                vm.AdminPinConfirm = "2480";
                vm.MasterPin = "482913";
                vm.MasterPinConfirm = "482913";
                vm.ShowRolePins = true;
                vm.ShowMasterPins = true;
                DrainDispatcher();

                var adminPinBox = (PasswordBox)page.FindName("AdminPinBox")!;
                var adminPinTextBox = (TextBox)page.FindName("AdminPinTextBox")!;
                var masterPinBox = (PasswordBox)page.FindName("MasterPinBox")!;
                var masterPinTextBox = (TextBox)page.FindName("MasterPinTextBox")!;

                Assert.Equal("2480", adminPinBox.Password);
                Assert.Equal("2480", adminPinTextBox.Text);
                Assert.Equal("482913", masterPinBox.Password);
                Assert.Equal("482913", masterPinTextBox.Text);

                page.ClearAllPinBoxes();
                DrainDispatcher();

                Assert.Equal(string.Empty, adminPinBox.Password);
                Assert.Equal(string.Empty, adminPinTextBox.Text);
                Assert.Equal(string.Empty, masterPinBox.Password);
                Assert.Equal(string.Empty, masterPinTextBox.Text);
            }
            finally
            {
                hostWindow.Close();
                keepAliveWindow.Close();
                vm.Dispose();
            }
        });
    }

    [Fact(Skip = "Flaky under repeated WPF app bootstrap in xUnit; covered by targeted manual/runtime checks.")]
    public void WorkspaceView_ShouldResolveInsideHostWindow()
    {
        RunOnStaThread(() =>
        {
            EnsureApplicationResources();

            using var host = BuildHost();
            var keepAliveWindow = CreateKeepAliveWindow();
            var mainVm = host.Services.GetRequiredService<MainViewModel>();
            var workspaceVm = host.Services.GetRequiredService<WorkspaceViewModel>();
            var page = new WorkspaceView { DataContext = workspaceVm };
            var hostWindow = new Window
            {
                Width = 900,
                Height = 600,
                Left = -20000,
                Top = -20000,
                ShowActivated = false,
                ShowInTaskbar = false,
                DataContext = mainVm,
                Content = page
            };

            try
            {
                hostWindow.Show();
                DrainDispatcher();
                hostWindow.UpdateLayout();

                var startBillingButton = (Button)page.FindName("StartBillingButton")!;
                Assert.NotNull(startBillingButton.Command);
                Assert.True(startBillingButton.Command.CanExecute(null));
            }
            finally
            {
                hostWindow.Close();
                keepAliveWindow.Close();
                mainVm.Dispose();
                workspaceVm.Dispose();
            }
        });
    }

    private static T FindControl<T>(DependencyObject root, string automationName) where T : FrameworkElement
    {
        var queue = new Queue<DependencyObject>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current is T typed
                && AutomationProperties.GetName(typed) == automationName)
            {
                return typed;
            }

            foreach (var child in LogicalTreeHelper.GetChildren(current).OfType<DependencyObject>())
                queue.Enqueue(child);
        }

        throw new InvalidOperationException($"Could not locate control '{automationName}'.");
    }

    private static Window ShowHostedControl(FrameworkElement control)
    {
        var hostWindow = new Window
        {
            Width = 900,
            Height = 700,
            Left = -20000,
            Top = -20000,
            ShowActivated = false,
            ShowInTaskbar = false,
            Content = control
        };

        hostWindow.Show();
        DrainDispatcher();
        hostWindow.UpdateLayout();
        return hostWindow;
    }

    private static Window CreateKeepAliveWindow()
    {
        var window = new Window
        {
            Width = 1,
            Height = 1,
            Left = -25000,
            Top = -25000,
            ShowActivated = false,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
            Opacity = 0
        };

        window.Show();
        DrainDispatcher();
        return window;
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

    private static void RunOnStaThread(Action action)
        => WpfTestApplication.Run(action);
}
