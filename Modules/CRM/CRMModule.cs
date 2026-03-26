using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.CRM.Services;

namespace StoreAssistantPro.Modules.CRM;

public static class CRMModule
{
    public static IServiceCollection AddCRMModule(this IServiceCollection services)
    {
        // DB-accessing services with no mutable state → Transient.
        services.AddTransient<ICampaignService, CampaignService>();
        services.AddTransient<IFeedbackService, FeedbackService>();
        services.AddTransient<IServiceTicketService, ServiceTicketService>();
        services.AddTransient<ICrmTemplateService, CrmTemplateService>();
        // Auto-greeting holds runtime greeting state → Singleton.
        services.AddSingleton<IAutoGreetingService, AutoGreetingService>();
        return services;
    }
}
