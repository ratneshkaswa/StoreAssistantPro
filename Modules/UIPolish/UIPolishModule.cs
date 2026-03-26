using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.UIPolish.Services;

namespace StoreAssistantPro.Modules.UIPolish;

public static class UIPolishModule
{
    public static IServiceCollection AddUIPolishModule(this IServiceCollection services)
    {
        // State-holding UI services → Singleton.
        services.AddSingleton<IAnimationService, AnimationService>();
        services.AddSingleton<ISkeletonService, SkeletonService>();
        services.AddSingleton<IProgressService, ProgressService>();
        services.AddSingleton<IIconService, IconService>();
        services.AddSingleton<IResponsiveLayoutService, ResponsiveLayoutService>();
        services.AddSingleton<IStatusBadgeService, StatusBadgeService>();
        services.AddSingleton<IChartService, ChartService>();
        return services;
    }
}
