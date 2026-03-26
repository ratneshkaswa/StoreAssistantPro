namespace StoreAssistantPro.Models.HR;

/// <summary>
/// Staff attendance record for clock in/out (#824).
/// </summary>
public sealed class AttendanceRecord
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime Date { get; set; }
    public DateTime? ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public double HoursWorked { get; set; }
    public double OvertimeHours { get; set; }
    public string Status { get; set; } = "Present"; // Present, Absent, Late, HalfDay, Leave
    public string? Notes { get; set; }
}

/// <summary>
/// Shift schedule definition (#825).
/// </summary>
public sealed class ShiftSchedule
{
    public int Id { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Staff shift assignment linking an employee to a shift for a date range (#825).
/// </summary>
public sealed class StaffShiftAssignment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ShiftScheduleId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

/// <summary>
/// Leave request and tracking (#827).
/// </summary>
public sealed class LeaveRequest
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string LeaveType { get; set; } = "CasualLeave"; // CasualLeave, SickLeave, Earned, Unpaid
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public int? ApprovedByUserId { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

/// <summary>
/// Staff performance metrics snapshot (#828).
/// </summary>
public sealed record StaffPerformanceMetrics(
    int UserId,
    string UserName,
    decimal SalesPerHour,
    double ItemsPerTransaction,
    int TotalTransactions,
    decimal TotalSalesAmount,
    double AverageTransactionTime,
    DateTime PeriodStart,
    DateTime PeriodEnd);

/// <summary>
/// Monthly sales target per employee (#829).
/// </summary>
public sealed class StaffTarget
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal AchievedAmount { get; set; }
    public bool IsAchieved => AchievedAmount >= TargetAmount;
    public double AchievementPercent => TargetAmount > 0 ? (double)(AchievedAmount / TargetAmount * 100) : 0;
}

/// <summary>
/// Payroll export record (#831).
/// </summary>
public sealed record PayrollExportRecord(
    int UserId,
    string EmployeeName,
    int Month,
    int Year,
    decimal BaseSalary,
    decimal Commission,
    decimal Overtime,
    decimal Deductions,
    decimal NetPayable,
    double TotalHoursWorked,
    int DaysPresent,
    int DaysAbsent,
    int LeaveDays);
