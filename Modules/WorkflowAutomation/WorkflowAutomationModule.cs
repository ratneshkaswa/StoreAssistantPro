using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.WorkflowAutomation.Services;

namespace StoreAssistantPro.Modules.WorkflowAutomation;

public static class WorkflowAutomationModule
{
    public static IServiceCollection AddWorkflowAutomationModule(this IServiceCollection services)
    {
        // DB-accessing service with no mutable state → Transient.
        services.AddTransient<IWorkflowAutomationService, WorkflowAutomationService>();
        return services;
    }
}
