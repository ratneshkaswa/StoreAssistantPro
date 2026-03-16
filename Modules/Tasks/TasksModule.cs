using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Tasks.Services;
using StoreAssistantPro.Modules.Tasks.ViewModels;

namespace StoreAssistantPro.Modules.Tasks;

public static class TasksModule
{
    public static IServiceCollection AddTasksModule(this IServiceCollection services)
    {
        services.AddTransient<ITaskService, TaskService>();
        services.AddTransient<TaskManagementViewModel>();
        return services;
    }
}
