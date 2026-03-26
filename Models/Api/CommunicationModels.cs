namespace StoreAssistantPro.Models.Api;

/// <summary>
/// Communication channel type for notifications.
/// </summary>
public enum CommunicationChannel
{
    Sms,
    Email,
    WhatsApp,
    PushNotification,
    Webhook,
    Slack,
    Teams
}

/// <summary>
/// Message to send through a communication channel.
/// </summary>
public sealed class CommunicationMessage
{
    public CommunicationChannel Channel { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? TemplateName { get; set; }
    public Dictionary<string, string> TemplateData { get; set; } = [];
    public string? AttachmentPath { get; set; }
}

/// <summary>
/// Result of a communication send attempt.
/// </summary>
public sealed record CommunicationResult(
    bool Success,
    CommunicationChannel Channel,
    string? MessageId,
    string? Error,
    DateTime SentAt);

/// <summary>
/// Notification template for configurable message content.
/// </summary>
public sealed class NotificationTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CommunicationChannel Channel { get; set; }
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Webhook event payload for outbound webhooks.
/// </summary>
public sealed record WebhookPayload(
    string EventType,
    string EventId,
    DateTime OccurredAt,
    object Data,
    string? Signature);

/// <summary>
/// Webhook endpoint configuration.
/// </summary>
public sealed class WebhookEndpoint
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string[] EventTypes { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
