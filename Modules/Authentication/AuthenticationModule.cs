using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Workflows;
using StoreAssistantPro.Modules.Authentication.Commands;
using StoreAssistantPro.Modules.Authentication.Services;
using StoreAssistantPro.Modules.Authentication.ViewModels;
using StoreAssistantPro.Modules.Authentication.Views;
using StoreAssistantPro.Modules.Authentication.Workflows;

namespace StoreAssistantPro.Modules.Authentication;

public static class AuthenticationModule
{
    public static IServiceCollection AddAuthenticationModule(this IServiceCollection services)
    {
        // Services
        services.AddTransient<ISetupService, SetupService>();
        services.AddTransient<ILoginService, LoginService>();
        services.AddSingleton<IAuthenticationFlow, AuthenticationFlow>();

        // Workflows
        services.AddSingleton<IWorkflow, LoginWorkflow>();

        // Command handlers
        services.AddTransient<ICommandRequestHandler<CompleteFirstSetupCommand, Unit>, CompleteFirstSetupHandler>();
        services.AddTransient<ICommandRequestHandler<LoginUserCommand, Unit>, LoginUserHandler>();
        services.AddTransient<ICommandRequestHandler<LogoutCommand, Unit>, LogoutHandler>();

        // ViewModels
        services.AddTransient<FirstTimeSetupViewModel>();
        services.AddTransient<UnifiedLoginViewModel>();

        // Views
        services.AddTransient<FirstTimeSetupWindow>();
        services.AddTransient<UnifiedLoginWindow>();

        return services;
    }
}
