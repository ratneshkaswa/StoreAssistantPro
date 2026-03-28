using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Users.Commands;
using StoreAssistantPro.Modules.Users.Services;
using StoreAssistantPro.Modules.Users.ViewModels;

namespace StoreAssistantPro.Modules.Users;

public static class UsersModule
{
    public const string UserManagementPage = "UserManagement";

    public static IServiceCollection AddUsersModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<UsersViewModel>(UserManagementPage)
            .RequireFeature(UserManagementPage, FeatureFlags.UserManagement);
        pageRegistry.CachePage(UserManagementPage);
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IPermissionService, PermissionService>();
        services.AddTransient<ICommandRequestHandler<ChangePinCommand, Unit>, ChangePinHandler>();
        services.AddTransient<UsersViewModel>();
        return services;
    }
}
