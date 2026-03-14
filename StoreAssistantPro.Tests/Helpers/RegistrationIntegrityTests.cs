using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Billing;
using StoreAssistantPro.Modules.Brands;
using StoreAssistantPro.Modules.Categories;
using StoreAssistantPro.Modules.Customers;
using StoreAssistantPro.Modules.FinancialYears;
using StoreAssistantPro.Modules.Firm;
using StoreAssistantPro.Modules.Inward;
using StoreAssistantPro.Modules.Inventory;
using StoreAssistantPro.Modules.MainShell;
using StoreAssistantPro.Modules.MainShell.ViewModels;
using StoreAssistantPro.Modules.Products;
using StoreAssistantPro.Modules.PurchaseOrders;
using StoreAssistantPro.Modules.Settings;
using StoreAssistantPro.Modules.Tax;
using StoreAssistantPro.Modules.Users;
using StoreAssistantPro.Modules.Vendors;

namespace StoreAssistantPro.Tests.Helpers;

public class RegistrationIntegrityTests
{
    private static readonly Type[] ModuleTypes =
    [
        typeof(BillingModule),
        typeof(BrandsModule),
        typeof(CategoriesModule),
        typeof(CustomersModule),
        typeof(FinancialYearsModule),
        typeof(FirmModule),
        typeof(InwardModule),
        typeof(InventoryModule),
        typeof(MainShellModule),
        typeof(ProductsModule),
        typeof(PurchaseOrdersModule),
        typeof(SettingsModule),
        typeof(TaxModule),
        typeof(UsersModule),
        typeof(VendorsModule)
    ];

    [Fact]
    public void ModuleDialogKeys_ShouldBeRegistered_InWindowRegistry()
    {
        using var host = BuildHost();
        var dialogMap = GetDialogMap(host.Services);

        var expectedKeys = ModuleTypes
            .SelectMany(GetPublicConstStrings)
            .Where(entry => entry.Field.Name.EndsWith("Dialog", StringComparison.Ordinal))
            .Select(entry => entry.Value)
            .ToHashSet(StringComparer.Ordinal);

        var missing = expectedKeys
            .Where(key => !dialogMap.ContainsKey(key))
            .OrderBy(key => key)
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"Dialog registration missing for keys:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    [Fact]
    public void MainViewModel_DialogKeys_ShouldExist_InWindowRegistry()
    {
        using var host = BuildHost();
        var dialogMap = GetDialogMap(host.Services);

        var missing = GetPrivateConstStrings(typeof(MainViewModel))
            .Where(entry => entry.Field.Name.EndsWith("Dialog", StringComparison.Ordinal))
            .Select(entry => entry.Value)
            .Where(key => !dialogMap.ContainsKey(key))
            .OrderBy(key => key)
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"MainViewModel references unregistered dialog keys:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    [Fact]
    public void MainShellPageKeys_ShouldExist_InNavigationService()
    {
        using var host = BuildHost();
        var pageMap = GetPageMap(host.Services);

        var modulePageKeys = ModuleTypes
            .SelectMany(GetPublicConstStrings)
            .Where(entry => entry.Field.Name.EndsWith("Page", StringComparison.Ordinal))
            .Select(entry => entry.Value);

        var mainViewModelPageKeys = GetPrivateConstStrings(typeof(MainViewModel))
            .Where(entry => entry.Field.Name.EndsWith("Page", StringComparison.Ordinal))
            .Select(entry => entry.Value);

        var missing = modulePageKeys
            .Concat(mainViewModelPageKeys)
            .Distinct(StringComparer.Ordinal)
            .Where(key => !pageMap.ContainsKey(key))
            .OrderBy(key => key)
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"Navigation registration missing for page keys:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    private static Dictionary<string, Type> GetDialogMap(IServiceProvider services)
    {
        var registry = services.GetRequiredService<IWindowRegistry>();
        var field = registry.GetType().GetField("_dialogMap", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate WindowRegistry._dialogMap.");

        return ((Dictionary<string, Type>)field.GetValue(registry)!)
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
    }

    private static Dictionary<string, Type> GetPageMap(IServiceProvider services)
    {
        var navigation = services.GetRequiredService<INavigationService>();
        var field = navigation.GetType().GetField("_pageMap", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate NavigationService._pageMap.");

        return ((Dictionary<string, Type>)field.GetValue(navigation)!)
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
    }

    private static IEnumerable<(FieldInfo Field, string Value)> GetPublicConstStrings(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => (field, (string)field.GetRawConstantValue()!));

    private static IEnumerable<(FieldInfo Field, string Value)> GetPrivateConstStrings(Type type) =>
        type.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => (field, (string)field.GetRawConstantValue()!));

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
        var navigationRegistry = host.Services.GetRequiredService<NavigationPageRegistry>();
        navigationRegistry.ApplyTo(host.Services.GetRequiredService<INavigationService>());
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
}
