using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.HR.Services;

namespace StoreAssistantPro.Modules.HR;

public static class HRModule
{
    public static IServiceCollection AddHRModule(this IServiceCollection services)
    {
        // DB-accessing services with no mutable state → Transient.
        services.AddTransient<IAttendanceService, AttendanceService>();
        services.AddTransient<IShiftScheduleService, ShiftScheduleService>();
        services.AddTransient<ILeaveService, LeaveService>();
        services.AddTransient<IStaffPerformanceService, StaffPerformanceService>();
        return services;
    }
}
