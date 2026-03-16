using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Authentication.Commands;
using StoreAssistantPro.Modules.Authentication.Services;
using StoreAssistantPro.Modules.Authentication.ViewModels;

namespace StoreAssistantPro.Modules.Authentication;

public static class AuthenticationModule
{
    public const string LoginPage = "Login";

    public static IServiceCollection AddAuthenticationModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        // Page registration
        pageRegistry.Map<LoginViewModel>(LoginPage);

        // Services
        services.AddTransient<ILoginService, LoginService>();

        // Command handlers
        services.AddTransient<ICommandRequestHandler<LoginUserCommand, Unit>, LoginUserHandler>();
        services.AddTransient<ICommandRequestHandler<LogoutCommand, Unit>, LogoutHandler>();

        // ViewModels
        services.AddTransient<LoginViewModel>();

        return services;
    }
}
