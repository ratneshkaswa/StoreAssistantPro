using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models.HR;

namespace StoreAssistantPro.Modules.HR.Events;

/// <summary>Published when a staff member clocks in.</summary>
public sealed class StaffClockInEvent(AttendanceRecord record) : IEvent
{
    public AttendanceRecord Record { get; } = record;
}

/// <summary>Published when a staff member clocks out.</summary>
public sealed class StaffClockOutEvent(AttendanceRecord record) : IEvent
{
    public AttendanceRecord Record { get; } = record;
}

/// <summary>Published when a leave request is submitted.</summary>
public sealed class LeaveRequestedEvent(LeaveRequest request) : IEvent
{
    public LeaveRequest Request { get; } = request;
}

/// <summary>Published when a leave request is approved or rejected.</summary>
public sealed class LeaveReviewedEvent(LeaveRequest request) : IEvent
{
    public LeaveRequest Request { get; } = request;
}
