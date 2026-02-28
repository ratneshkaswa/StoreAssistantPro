using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Users.Commands;
using StoreAssistantPro.Modules.Users.Services;
using StoreAssistantPro.Modules.Users.ViewModels;
using StoreAssistantPro.Modules.Users.Views;

namespace StoreAssistantPro.Modules.Users;

public static class UsersModule
{
    public const string UserManagementDialog = "UserManagement";

    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        // Services
        services.AddTransient<IUserService, UserService>();

        // Command handlers
        services.AddTransient<ICommandRequestHandler<ChangePinCommand, Unit>, ChangePinHandler>();

        // ViewModels
        services.AddTransient<UsersViewModel>();

        // Views
        services.AddTransient<UsersWindow>();

        // Dialog registration
        services.AddDialogRegistration<UsersWindow>(UserManagementDialog);

        return services;
    }
}
