using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Data;
using StoreAssistantPro.Services;
using StoreAssistantPro.ViewModels;
using StoreAssistantPro.Views;

namespace StoreAssistantPro;

internal static class HostingExtensions
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null)));

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<ISalesService, SalesService>();
        services.AddSingleton<IStartupService, StartupService>();
        services.AddSingleton<ISetupService, SetupService>();
        services.AddSingleton<ILoginService, LoginService>();

        return services;
    }

    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<SalesViewModel>();
        services.AddTransient<FirstTimeSetupViewModel>();
        services.AddTransient<UserSelectionViewModel>();
        services.AddTransient<PinLoginViewModel>();

        return services;
    }

    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddTransient<FirstTimeSetupWindow>();
        services.AddTransient<UserSelectionWindow>();
        services.AddTransient<PinLoginWindow>();

        return services;
    }
}
