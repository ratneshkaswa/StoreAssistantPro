using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Modules.AdvancedReporting.Events;

/// <summary>Published when a custom report is generated.</summary>
public sealed class ReportGeneratedEvent(int reportId, string reportName) : IEvent
{
    public int ReportId { get; } = reportId;
    public string ReportName { get; } = reportName;
}

/// <summary>Published when a scheduled report is sent.</summary>
public sealed class ScheduledReportSentEvent(int scheduleId, int reportId) : IEvent
{
    public int ScheduleId { get; } = scheduleId;
    public int ReportId { get; } = reportId;
}
