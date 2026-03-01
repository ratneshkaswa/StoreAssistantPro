using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Staff.Commands;
using StoreAssistantPro.Modules.Staff.Services;
using StoreAssistantPro.Modules.Staff.ViewModels;

namespace StoreAssistantPro.Modules.Staff;

public static class StaffModule
{
    public const string StaffPage = "Staff";

    public static IServiceCollection AddStaffModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<StaffViewModel>(StaffPage);

        services.AddSingleton<IStaffService, StaffService>();
        services.AddSingleton<IIncentiveService, IncentiveService>();
        services.AddTransient<ICommandRequestHandler<SaveStaffCommand, Unit>, SaveStaffHandler>();
        services.AddTransient<ICommandRequestHandler<DeleteStaffCommand, Unit>, DeleteStaffHandler>();
        services.AddTransient<StaffViewModel>();

        return services;
    }
}
