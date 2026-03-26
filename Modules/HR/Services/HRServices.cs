using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.HR;

namespace StoreAssistantPro.Modules.HR.Services;

public sealed class AttendanceService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<AttendanceService> logger) : IAttendanceService
{
    public async Task<AttendanceRecord> ClockInAsync(int userId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var today = DateTime.Today;
        var existing = await context.Set<AttendanceRecord>()
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == today, ct).ConfigureAwait(false);

        if (existing is not null)
        {
            logger.LogWarning("User {UserId} already clocked in today", userId);
            return existing;
        }

        var record = new AttendanceRecord
        {
            UserId = userId,
            Date = today,
            ClockIn = DateTime.UtcNow,
            Status = "Present"
        };
        context.Set<AttendanceRecord>().Add(record);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("User {UserId} clocked in at {Time}", userId, record.ClockIn);
        return record;
    }

    public async Task<AttendanceRecord> ClockOutAsync(int userId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var today = DateTime.Today;
        var record = await context.Set<AttendanceRecord>()
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == today, ct).ConfigureAwait(false);

        if (record is null)
        {
            logger.LogWarning("User {UserId} has no clock-in record for today", userId);
            record = new AttendanceRecord { UserId = userId, Date = today, ClockIn = DateTime.UtcNow, Status = "Present" };
            context.Set<AttendanceRecord>().Add(record);
        }

        record.ClockOut = DateTime.UtcNow;
        if (record.ClockIn.HasValue)
            record.HoursWorked = (record.ClockOut.Value - record.ClockIn.Value).TotalHours;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("User {UserId} clocked out. Hours: {Hours:F1}", userId, record.HoursWorked);
        return record;
    }

    public async Task<AttendanceRecord?> GetTodayAttendanceAsync(int userId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Set<AttendanceRecord>()
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == DateTime.Today, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetAttendanceHistoryAsync(int userId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Set<AttendanceRecord>()
            .Where(a => a.UserId == userId && a.Date >= from.Date && a.Date <= to.Date)
            .OrderByDescending(a => a.Date).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetAllAttendanceForDateAsync(DateTime date, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Set<AttendanceRecord>()
            .Where(a => a.Date == date.Date).OrderBy(a => a.UserId).ToListAsync(ct).ConfigureAwait(false);
    }
}

public sealed class ShiftScheduleService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<ShiftScheduleService> logger) : IShiftScheduleService
{
    public async Task<IReadOnlyList<ShiftSchedule>> GetShiftsAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Set<ShiftSchedule>().Where(s => s.IsActive).OrderBy(s => s.StartTime).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<ShiftSchedule> CreateShiftAsync(ShiftSchedule shift, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.Set<ShiftSchedule>().Add(shift);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Created shift: {Name} ({Start}-{End})", shift.ShiftName, shift.StartTime, shift.EndTime);
        return shift;
    }

    public async Task UpdateShiftAsync(ShiftSchedule shift, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.Set<ShiftSchedule>().Update(shift);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteShiftAsync(int shiftId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var shift = await context.Set<ShiftSchedule>().FindAsync([shiftId], ct).ConfigureAwait(false);
        if (shift is not null)
        {
            shift.IsActive = false;
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task AssignShiftAsync(int userId, int shiftId, DateTime effectiveFrom, DateTime? effectiveTo = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.Set<StaffShiftAssignment>().Add(new StaffShiftAssignment
        {
            UserId = userId, ShiftScheduleId = shiftId, EffectiveFrom = effectiveFrom, EffectiveTo = effectiveTo
        });
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Assigned shift {ShiftId} to user {UserId}", shiftId, userId);
    }

    public async Task<IReadOnlyList<StaffShiftAssignment>> GetAssignmentsAsync(int? userId = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Set<StaffShiftAssignment>().AsQueryable();
        if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);
        return await query.OrderByDescending(a => a.EffectiveFrom).ToListAsync(ct).ConfigureAwait(false);
    }
}

public sealed class LeaveService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<LeaveService> logger) : ILeaveService
{
    public async Task<LeaveRequest> RequestLeaveAsync(LeaveRequest request, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        request.Status = "Pending";
        request.RequestedAt = DateTime.UtcNow;
        context.Set<LeaveRequest>().Add(request);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Leave request created for user {UserId}: {Type} from {From} to {To}", request.UserId, request.LeaveType, request.StartDate, request.EndDate);
        return request;
    }

    public async Task<LeaveRequest> ApproveLeaveAsync(int leaveRequestId, int approverUserId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var request = await context.Set<LeaveRequest>().FindAsync([leaveRequestId], ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Leave request {leaveRequestId} not found");
        request.Status = "Approved";
        request.ApprovedByUserId = approverUserId;
        request.ReviewedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Leave request {Id} approved by user {Approver}", leaveRequestId, approverUserId);
        return request;
    }

    public async Task<LeaveRequest> RejectLeaveAsync(int leaveRequestId, int approverUserId, string? reason = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var request = await context.Set<LeaveRequest>().FindAsync([leaveRequestId], ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Leave request {leaveRequestId} not found");
        request.Status = "Rejected";
        request.ApprovedByUserId = approverUserId;
        request.ReviewedAt = DateTime.UtcNow;
        if (reason is not null) request.Reason = reason;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return request;
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetLeaveRequestsAsync(int? userId = null, string? status = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Set<LeaveRequest>().AsQueryable();
        if (userId.HasValue) query = query.Where(r => r.UserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(r => r.Status == status);
        return await query.OrderByDescending(r => r.RequestedAt).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<int> GetLeaveBalanceAsync(int userId, string leaveType, int year, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var used = await context.Set<LeaveRequest>()
            .CountAsync(r => r.UserId == userId && r.LeaveType == leaveType && r.Status == "Approved"
                && r.StartDate.Year == year, ct).ConfigureAwait(false);
        var maxLeaves = leaveType switch { "CasualLeave" => 12, "SickLeave" => 10, "Earned" => 15, _ => 0 };
        return maxLeaves - used;
    }
}

public sealed class StaffPerformanceService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<StaffPerformanceService> logger) : IStaffPerformanceService
{
    public async Task<StaffPerformanceMetrics> GetPerformanceAsync(int userId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var sales = await context.Sales.Where(s => s.StaffId == userId && s.SaleDate >= from && s.SaleDate <= to)
            .Select(s => new { s.TotalAmount, ItemCount = s.Items.Count }).ToListAsync(ct).ConfigureAwait(false);

        var totalHours = await context.Set<AttendanceRecord>()
            .Where(a => a.UserId == userId && a.Date >= from.Date && a.Date <= to.Date)
            .SumAsync(a => a.HoursWorked, ct).ConfigureAwait(false);

        var userName = await context.UserCredentials.Where(u => u.Id == userId)
            .Select(u => u.DisplayName ?? u.UserType.ToString()).FirstOrDefaultAsync(ct).ConfigureAwait(false);

        return new StaffPerformanceMetrics(
            userId, userName ?? $"User #{userId}",
            totalHours > 0 ? sales.Sum(s => s.TotalAmount) / (decimal)totalHours : 0,
            sales.Count > 0 ? sales.Average(s => s.ItemCount) : 0,
            sales.Count, sales.Sum(s => s.TotalAmount), 0, from, to);
    }

    public async Task<decimal> CalculateCommissionAsync(int userId, int month, int year, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1);
        var total = await context.Sales.Where(s => s.StaffId == userId && s.SaleDate >= from && s.SaleDate < to)
            .SumAsync(s => s.TotalAmount, ct).ConfigureAwait(false);
        var commissionRate = 0.02m; // 2% default
        return total * commissionRate;
    }

    public async Task SetTargetAsync(StaffTarget target, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = await context.Set<StaffTarget>()
            .FirstOrDefaultAsync(t => t.UserId == target.UserId && t.Month == target.Month && t.Year == target.Year, ct)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            existing.TargetAmount = target.TargetAmount;
            existing.AchievedAmount = target.AchievedAmount;
        }
        else
        {
            context.Set<StaffTarget>().Add(target);
        }
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Set target for user {UserId}: ₹{Target} for {Month}/{Year}", target.UserId, target.TargetAmount, target.Month, target.Year);
    }

    public async Task<StaffTarget?> GetTargetAsync(int userId, int month, int year, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var target = await context.Set<StaffTarget>()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Month == month && t.Year == year, ct).ConfigureAwait(false);
        if (target is null) return null;

        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1);
        target.AchievedAmount = await context.Sales
            .Where(s => s.StaffId == userId && s.SaleDate >= from && s.SaleDate < to)
            .SumAsync(s => s.TotalAmount, ct).ConfigureAwait(false);
        return target;
    }

    public async Task<double> CalculateOvertimeAsync(int userId, int month, int year, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1);
        return await context.Set<AttendanceRecord>()
            .Where(a => a.UserId == userId && a.Date >= from && a.Date < to)
            .SumAsync(a => a.OvertimeHours, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PayrollExportRecord>> GeneratePayrollAsync(int month, int year, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1);
        var daysInMonth = DateTime.DaysInMonth(year, month);

        var users = await context.UserCredentials.Select(u => new { u.Id, Name = u.DisplayName ?? u.UserType.ToString() }).ToListAsync(ct).ConfigureAwait(false);
        var results = new List<PayrollExportRecord>();

        foreach (var user in users)
        {
            var attendance = await context.Set<AttendanceRecord>()
                .Where(a => a.UserId == user.Id && a.Date >= from && a.Date < to).ToListAsync(ct).ConfigureAwait(false);

            var salary = await context.Salaries
                .Where(s => s.EmployeeName == user.Name && s.Year == year)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);

            var commission = await CalculateCommissionAsync(user.Id, month, year, ct).ConfigureAwait(false);
            var overtime = attendance.Sum(a => a.OvertimeHours);
            var daysPresent = attendance.Count(a => a.Status == "Present");

            results.Add(new PayrollExportRecord(
                user.Id, user.Name, month, year,
                salary?.BaseSalary ?? 0, commission, (decimal)overtime * 200,
                salary?.Advance ?? 0,
                (salary?.BaseSalary ?? 0) + commission + (decimal)overtime * 200 - (salary?.Advance ?? 0),
                attendance.Sum(a => a.HoursWorked), daysPresent,
                daysInMonth - daysPresent - attendance.Count(a => a.Status == "Leave"),
                attendance.Count(a => a.Status == "Leave")));
        }

        return results;
    }

    public async Task<string> ExportPayrollCsvAsync(int month, int year, string outputPath, CancellationToken ct = default)
    {
        var records = await GeneratePayrollAsync(month, year, ct).ConfigureAwait(false);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("UserId,EmployeeName,Month,Year,BaseSalary,Commission,Overtime,Deductions,NetPayable,TotalHours,DaysPresent,DaysAbsent,LeaveDays");
        foreach (var r in records)
            sb.AppendLine($"{r.UserId},{r.EmployeeName},{r.Month},{r.Year},{r.BaseSalary},{r.Commission},{r.Overtime},{r.Deductions},{r.NetPayable},{r.TotalHoursWorked:F1},{r.DaysPresent},{r.DaysAbsent},{r.LeaveDays}");

        await System.IO.File.WriteAllTextAsync(outputPath, sb.ToString(), ct).ConfigureAwait(false);
        logger.LogInformation("Exported payroll for {Month}/{Year} to {Path}", month, year, outputPath);
        return outputPath;
    }
}
