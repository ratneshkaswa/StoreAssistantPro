using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Navigation;

namespace StoreAssistantPro.Tests.Helpers;

[Collection("WpfUi")]
public class RuntimeConstructibilityTests
{
    private static readonly string[] AuditedSuffixes =
    [
        "Service",
        "Flow",
        "Workflow",
        "Handler",
        "ViewModel",
        "Registry",
        "Manager",
        "Bus",
        "Monitor",
        "Engine",
        "Guard",
        "Policy",
        "Validator",
        "Tracker",
        "Registrar"
    ];

    [Fact(Skip = "Run this host-bootstrapped WPF constructibility audit individually; it is not stable after the full xUnit run.")]
    public void RegisteredRuntimeGraph_ShouldResolve_AndViewModelCommandsShouldBeStable()
    {
        var failures = RunOnStaThread(() =>
        {
            EnsureApplicationResources();

            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                ContentRootPath = FindContentRoot()
            });

            InvokeHostingExtension("AddDataAccess", builder.Services, builder.Configuration);
            InvokeHostingExtension("AddCoreServices", builder.Services);
            InvokeHostingExtension("AddModules", builder.Services);

            var descriptors = builder.Services.ToArray();

            using var host = builder.Build();
            host.Services
                .GetRequiredService<NavigationPageRegistry>()
                .ApplyTo(host.Services.GetRequiredService<INavigationService>());
            InvokeHostingExtension("ApplyDialogRegistrations", host.Services);
            BaseViewModel.SetLoggerFactory(host.Services.GetRequiredService<ILoggerFactory>());

            var runtimeFailures = new List<string>();

            try
            {
                foreach (var group in GetAuditedServiceDescriptors(descriptors)
                             .GroupBy(descriptor => descriptor.ServiceType)
                             .OrderBy(group => group.Key.FullName, StringComparer.Ordinal))
                {
                    AuditServiceGroup(host.Services, group.Key, group.ToArray(), runtimeFailures);
                }

                foreach (var descriptor in GetViewModelDescriptors(descriptors)
                             .DistinctBy(descriptor => descriptor.ServiceType)
                             .OrderBy(descriptor => descriptor.ServiceType.FullName, StringComparer.Ordinal))
                {
                    AuditViewModel(host.Services, descriptor.ServiceType, runtimeFailures);
                }
            }
            finally
            {
                BaseViewModel.SetLoggerFactory(NullLoggerFactory.Instance);
            }

            return runtimeFailures;
        });

        Assert.True(
            failures.Count == 0,
            $"Runtime constructibility audit failed:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    private static void AuditServiceGroup(
        IServiceProvider services,
        Type serviceType,
        IReadOnlyList<ServiceDescriptor> descriptors,
        List<string> failures)
    {
        try
        {
            var instances = descriptors.Count == 1
                ? [services.GetRequiredService(serviceType)]
                : services.GetServices(serviceType).Cast<object>().ToArray();

            if (instances.Length == 0)
            {
                failures.Add($"{serviceType.Name}: resolution returned no instances.");
                return;
            }

            foreach (var descriptor in descriptors)
            {
                var expectedType = descriptor.ImplementationType ?? descriptor.ImplementationInstance?.GetType();
                if (expectedType is null)
                    continue;

                if (!instances.Any(expectedType.IsInstanceOfType))
                {
                    failures.Add(
                        $"{serviceType.Name}: expected implementation {expectedType.Name} was not resolved from DI.");
                }
            }
        }
        catch (Exception ex)
        {
            failures.Add($"{serviceType.Name}: {ex.GetType().Name} - {ex.Message}");
        }
    }

    private static void AuditViewModel(IServiceProvider services, Type viewModelType, List<string> failures)
    {
        object? viewModel = null;

        try
        {
            viewModel = services.GetRequiredService(viewModelType);

            foreach (var property in viewModelType.GetProperties()
                         .Where(property =>
                             property.CanRead
                             && property.GetIndexParameters().Length == 0
                             && typeof(ICommand).IsAssignableFrom(property.PropertyType))
                         .OrderBy(property => property.Name, StringComparer.Ordinal))
            {
                var command = property.GetValue(viewModel) as ICommand;
                if (command is null)
                {
                    failures.Add($"{viewModelType.Name}.{property.Name}: command property resolved to null.");
                    continue;
                }

                try
                {
                    _ = command.CanExecute(null);
                }
                catch (Exception ex)
                {
                    failures.Add(
                        $"{viewModelType.Name}.{property.Name}: CanExecute(null) threw {ex.GetType().Name} - {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            failures.Add($"{viewModelType.Name}: {ex.GetType().Name} - {ex.Message}");
        }
        finally
        {
            try
            {
                (viewModel as IDisposable)?.Dispose();
            }
            catch (Exception ex)
            {
                failures.Add($"{viewModelType.Name}: Dispose threw {ex.GetType().Name} - {ex.Message}");
            }
        }
    }

    private static IEnumerable<ServiceDescriptor> GetAuditedServiceDescriptors(IEnumerable<ServiceDescriptor> descriptors) =>
        descriptors.Where(descriptor =>
        {
            if (IsViewModelDescriptor(descriptor))
                return false;

            var implementationType = GetImplementationType(descriptor);
            if (implementationType is null)
                return false;

            return ShouldAuditType(descriptor.ServiceType, implementationType);
        });

    private static IEnumerable<ServiceDescriptor> GetViewModelDescriptors(IEnumerable<ServiceDescriptor> descriptors) =>
        descriptors.Where(IsViewModelDescriptor);

    private static bool IsViewModelDescriptor(ServiceDescriptor descriptor)
    {
        var implementationType = GetImplementationType(descriptor);
        return implementationType is not null
               && descriptor.ServiceType == implementationType
               && implementationType.Name.EndsWith("ViewModel", StringComparison.Ordinal);
    }

    private static Type? GetImplementationType(ServiceDescriptor descriptor)
    {
        var implementationType = descriptor.ImplementationType ?? descriptor.ImplementationInstance?.GetType();
        if (implementationType is null && descriptor.ImplementationFactory is not null)
            implementationType = descriptor.ServiceType;

        return implementationType;
    }

    private static bool ShouldAuditType(Type serviceType, Type implementationType)
    {
        if (serviceType.IsGenericTypeDefinition || implementationType.IsGenericTypeDefinition)
            return false;

        if (serviceType.ContainsGenericParameters || implementationType.ContainsGenericParameters)
            return false;

        if (implementationType.Assembly != typeof(App).Assembly)
            return false;

        if (typeof(Window).IsAssignableFrom(implementationType)
            || typeof(Page).IsAssignableFrom(implementationType))
        {
            return false;
        }

        if (serviceType.FullName == "StoreAssistantPro.DialogRegistration")
            return false;

        return AuditedSuffixes.Any(suffix =>
            implementationType.Name.EndsWith(suffix, StringComparison.Ordinal));
    }

    private static object? InvokeHostingExtension(string methodName, params object[] arguments)
    {
        var hostingExtensions = typeof(App).Assembly.GetType("StoreAssistantPro.HostingExtensions")
            ?? throw new InvalidOperationException("Could not locate HostingExtensions.");

        var method = hostingExtensions
            .GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
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
