using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Tasks.Services;
using StoreAssistantPro.Modules.Tasks.ViewModels;
using StoreAssistantPro.Modules.Tasks.Views;

namespace StoreAssistantPro.Modules.Tasks;

public static class TasksModule
{
    public const string TaskManagementDialog = "TaskManagement";

    public static IServiceCollection AddTasksModule(this IServiceCollection services)
    {
        services.AddTransient<ITaskService, TaskService>();
        services.AddTransient<TaskManagementViewModel>();
        services.AddTransient<TaskManagementWindow>();
        services.AddDialogRegistration<TaskManagementWindow>(TaskManagementDialog);
        return services;
    }
}
