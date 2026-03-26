namespace StoreAssistantPro.Models.CRM;

/// <summary>Marketing campaign (#799-808).</summary>
public sealed class Campaign
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Channel { get; set; } = "SMS"; // SMS, Email, WhatsApp
    public string? TemplateContent { get; set; }
    public string? TargetSegment { get; set; }
    public int RecipientCount { get; set; }
    public int SentCount { get; set; }
    public int OpenCount { get; set; }
    public int ConversionCount { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Scheduled, Sending, Completed
}

/// <summary>Customer complaint/service request (#809-818).</summary>
public sealed class ServiceTicket
{
    public int Id { get; set; }
    public int? CustomerId { get; set; }
    public string TicketType { get; set; } = "Complaint"; // Complaint, ServiceRequest, Warranty, Support
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Open"; // Open, InProgress, Resolved, Closed, Escalated
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
    public int? AssignedToUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? EscalationDeadline { get; set; }
    public string? ResolutionNotes { get; set; }
    public int? SatisfactionScore { get; set; } // 1-5 CSAT
}

/// <summary>Feedback / NPS entry (#804-805).</summary>
public sealed record CustomerFeedback(
    int CustomerId,
    int? SaleId,
    int Score,
    string? Comment,
    string FeedbackType,
    DateTime SubmittedAt);

/// <summary>CRM message template (#819-823).</summary>
public sealed class CrmTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Channel { get; set; } = "SMS";
    public string Content { get; set; } = string.Empty;
    public string? MergeFields { get; set; } // JSON list of available merge fields
    public bool IsActive { get; set; } = true;
}
