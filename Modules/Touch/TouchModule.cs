using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Touch.Services;

namespace StoreAssistantPro.Modules.Touch;

public static class TouchModule
{
    public static IServiceCollection AddTouchModule(this IServiceCollection services)
    {
        // Touch/kiosk mode holds UI state → Singleton.
        services.AddSingleton<ITouchModeService, TouchModeService>();
        services.AddSingleton<IGestureService, GestureService>();
        services.AddSingleton<IOnScreenInputService, OnScreenInputService>();
        return services;
    }
}
