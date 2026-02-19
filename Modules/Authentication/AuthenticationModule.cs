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
        services.AddSingleton<ISetupService, SetupService>();
        services.AddSingleton<ILoginService, LoginService>();
        services.AddSingleton<IAuthenticationFlow, AuthenticationFlow>();

        // Workflows
        services.AddSingleton<IWorkflow, LoginWorkflow>();

        // Command handlers
        services.AddTransient<ICommandHandler<CompleteFirstSetupCommand>, CompleteFirstSetupHandler>();
        services.AddTransient<ICommandHandler<LoginUserCommand>, LoginUserHandler>();
        services.AddTransient<ICommandHandler<LogoutCommand>, LogoutHandler>();

        // ViewModels
        services.AddTransient<FirstTimeSetupViewModel>();
        services.AddTransient<UserSelectionViewModel>();
        services.AddTransient<PinLoginViewModel>();

        // Views
        services.AddTransient<FirstTimeSetupWindow>();
        services.AddTransient<UserSelectionWindow>();
        services.AddTransient<PinLoginWindow>();

        return services;
    }
}
