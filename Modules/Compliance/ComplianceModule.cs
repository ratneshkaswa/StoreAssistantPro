using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Compliance.Services;

namespace StoreAssistantPro.Modules.Compliance;

public static class ComplianceModule
{
    public static IServiceCollection AddComplianceModule(this IServiceCollection services)
    {
        // DB-accessing services with no mutable state → Transient.
        services.AddTransient<IGstReturnService, GstReturnService>();
        services.AddTransient<IEWayBillService, EWayBillService>();
        services.AddTransient<IEInvoiceService, EInvoiceService>();
        services.AddTransient<IDataComplianceService, DataComplianceService>();
        return services;
    }
}
