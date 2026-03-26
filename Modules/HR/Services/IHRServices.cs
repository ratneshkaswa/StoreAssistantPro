using StoreAssistantPro.Models.HR;

namespace StoreAssistantPro.Modules.HR.Services;

/// <summary>
/// Staff attendance and clock in/out service (#824).
/// </summary>
public interface IAttendanceService
{
    Task<AttendanceRecord> ClockInAsync(int userId, CancellationToken ct = default);
    Task<AttendanceRecord> ClockOutAsync(int userId, CancellationToken ct = default);
    Task<AttendanceRecord?> GetTodayAttendanceAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceRecord>> GetAttendanceHistoryAsync(int userId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<AttendanceRecord>> GetAllAttendanceForDateAsync(DateTime date, CancellationToken ct = default);
}

/// <summary>
/// Shift scheduling service (#825).
/// </summary>
public interface IShiftScheduleService
{
    Task<IReadOnlyList<ShiftSchedule>> GetShiftsAsync(CancellationToken ct = default);
    Task<ShiftSchedule> CreateShiftAsync(ShiftSchedule shift, CancellationToken ct = default);
    Task UpdateShiftAsync(ShiftSchedule shift, CancellationToken ct = default);
    Task DeleteShiftAsync(int shiftId, CancellationToken ct = default);
    Task AssignShiftAsync(int userId, int shiftId, DateTime effectiveFrom, DateTime? effectiveTo = null, CancellationToken ct = default);
    Task<IReadOnlyList<StaffShiftAssignment>> GetAssignmentsAsync(int? userId = null, CancellationToken ct = default);
}

/// <summary>
/// Leave management service (#827).
/// </summary>
public interface ILeaveService
{
    Task<LeaveRequest> RequestLeaveAsync(LeaveRequest request, CancellationToken ct = default);
    Task<LeaveRequest> ApproveLeaveAsync(int leaveRequestId, int approverUserId, CancellationToken ct = default);
    Task<LeaveRequest> RejectLeaveAsync(int leaveRequestId, int approverUserId, string? reason = null, CancellationToken ct = default);
    Task<IReadOnlyList<LeaveRequest>> GetLeaveRequestsAsync(int? userId = null, string? status = null, CancellationToken ct = default);
    Task<int> GetLeaveBalanceAsync(int userId, string leaveType, int year, CancellationToken ct = default);
}

/// <summary>
/// Staff performance and target tracking service (#826, #828, #829, #830).
/// </summary>
public interface IStaffPerformanceService
{
    /// <summary>Get performance metrics for a staff member (#828).</summary>
    Task<StaffPerformanceMetrics> GetPerformanceAsync(int userId, DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Get commission calculated for a staff member (#826).</summary>
    Task<decimal> CalculateCommissionAsync(int userId, int month, int year, CancellationToken ct = default);

    /// <summary>Set monthly sales target (#829).</summary>
    Task SetTargetAsync(StaffTarget target, CancellationToken ct = default);

    /// <summary>Get monthly sales target and achievement (#829).</summary>
    Task<StaffTarget?> GetTargetAsync(int userId, int month, int year, CancellationToken ct = default);

    /// <summary>Calculate overtime hours beyond scheduled shift (#830).</summary>
    Task<double> CalculateOvertimeAsync(int userId, int month, int year, CancellationToken ct = default);

    /// <summary>Generate payroll export data (#831).</summary>
    Task<IReadOnlyList<PayrollExportRecord>> GeneratePayrollAsync(int month, int year, CancellationToken ct = default);

    /// <summary>Export payroll to CSV (#831).</summary>
    Task<string> ExportPayrollCsvAsync(int month, int year, string outputPath, CancellationToken ct = default);
}
