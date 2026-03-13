using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core;
using StoreAssistantPro.Modules.Billing.Views;
using StoreAssistantPro.Modules.Brands.Views;
using StoreAssistantPro.Modules.Categories.Views;
using StoreAssistantPro.Modules.Customers.Views;
using StoreAssistantPro.Modules.FinancialYears.Views;
using StoreAssistantPro.Modules.Firm.Views;
using StoreAssistantPro.Modules.Inventory.Views;
using StoreAssistantPro.Modules.Inward.Views;
using StoreAssistantPro.Modules.Products.Views;
using StoreAssistantPro.Modules.PurchaseOrders.Views;
using StoreAssistantPro.Modules.Settings.Views;
using StoreAssistantPro.Modules.Tax.Views;
using StoreAssistantPro.Modules.Users.Views;
using StoreAssistantPro.Modules.Vendors.Views;

namespace StoreAssistantPro.Tests.Helpers;

[Collection("WpfUi")]
public class MainMenuDialogResolutionTests
{
    private static readonly Type[] QuickAccessWindowTypes =
    [
        typeof(FirmWindow),
        typeof(UsersWindow),
        typeof(TaxManagementWindow),
        typeof(VendorManagementWindow),
        typeof(ProductManagementWindow),
        typeof(CategoryManagementWindow),
        typeof(BrandManagementWindow),
        typeof(FinancialYearWindow),
        typeof(SystemSettingsWindow),
        typeof(InwardEntryWindow),
        typeof(InventoryWindow),
        typeof(BillingWindow),
        typeof(SaleHistoryWindow),
        typeof(CustomerManagementWindow),
        typeof(PurchaseOrderWindow)
    ];

    [Fact]
    public void QuickAccessWindows_ShouldResolve_WithoutConstructorOrXamlFailures()
    {
        var failures = RunOnStaThread(() =>
        {
            EnsureApplicationResources();

            using var host = BuildHost();
            var failures = new List<string>();

            foreach (var windowType in QuickAccessWindowTypes)
            {
                var exception = Record.Exception(() =>
                {
                    var window = (Window)host.Services.GetRequiredService(windowType);
                    window.ApplyTemplate();
                    _ = window.Content;
                    _ = window.DataContext;
                });

                if (exception is not null)
                    failures.Add($"{windowType.Name}: {exception.GetType().Name} - {exception.Message}");
            }

            return failures;
        });

        Assert.True(
            failures.Count == 0,
            $"Quick-access windows failed to resolve:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
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
    {
        if (Application.Current is App)
            return;

        if (Application.Current is not null)
            throw new InvalidOperationException("A non-StoreAssistantPro WPF application is already active in the test AppDomain.");

        var app = new App
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };

        app.InitializeComponent();
    }

    private static T RunOnStaThread<T>(Func<T> action)
    {
        T? result = default;
        Exception? exception = null;

        var thread = new Thread(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                Application.Current?.Dispatcher.Invoke(() => Application.Current.Shutdown());
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
            ExceptionDispatchInfo.Capture(exception).Throw();

        return result!;
    }
}
