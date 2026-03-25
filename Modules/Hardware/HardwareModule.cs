using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Hardware.Services;

namespace StoreAssistantPro.Modules.Hardware;

public static class HardwareModule
{
    public static IServiceCollection AddHardwareModule(this IServiceCollection services)
    {
        // Hardware services hold device state (connections, ports) → Singleton.
        services.AddSingleton<IBarcodeScannerService, BarcodeScannerService>();
        services.AddSingleton<IThermalPrinterService, ThermalPrinterService>();
        services.AddSingleton<ICashDrawerService, CashDrawerService>();
        services.AddSingleton<IWeighingScaleService, WeighingScaleService>();
        services.AddSingleton<ILabelPrinterService, LabelPrinterService>();
        services.AddSingleton<ICustomerDisplayService, CustomerDisplayService>();
        return services;
    }
}
