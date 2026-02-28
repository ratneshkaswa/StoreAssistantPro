using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Staff.Commands;
using StoreAssistantPro.Modules.Staff.Services;

namespace StoreAssistantPro.Modules.Staff;

public static class StaffModule
{
    public static IServiceCollection AddStaffModule(this IServiceCollection services)
    {
        services.AddSingleton<IStaffService, StaffService>();
        services.AddTransient<ICommandRequestHandler<SaveStaffCommand, Unit>, SaveStaffHandler>();
        services.AddTransient<ICommandRequestHandler<DeleteStaffCommand, Unit>, DeleteStaffHandler>();

        return services;
    }
}
