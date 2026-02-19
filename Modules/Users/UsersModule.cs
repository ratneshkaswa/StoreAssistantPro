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
        services.AddSingleton<IUserService, UserService>();

        // Command handlers
        services.AddTransient<ICommandHandler<ChangePinCommand>, ChangePinHandler>();

        // ViewModels
        services.AddTransient<UserManagementViewModel>();

        // Views
        services.AddTransient<UserManagementWindow>();

        // Dialog registration
        services.AddDialogRegistration<UserManagementWindow>(UserManagementDialog);

        return services;
    }
}
