using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Inward.Services;

namespace StoreAssistantPro.Modules.Inward;

public static class InwardModule
{
    public static IServiceCollection AddInwardModule(this IServiceCollection services)
    {
        services.AddTransient<IInwardService, InwardService>();
        return services;
    }
}
