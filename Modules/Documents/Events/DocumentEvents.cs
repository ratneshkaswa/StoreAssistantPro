using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models.Documents;

namespace StoreAssistantPro.Modules.Documents.Events;

/// <summary>Published when a document is generated.</summary>
public sealed class DocumentGeneratedEvent(string documentType, string filePath) : IEvent
{
    public string DocumentType { get; } = documentType;
    public string FilePath { get; } = filePath;
}

/// <summary>Published when a document is emailed.</summary>
public sealed class DocumentEmailedEvent(int documentId, string recipientEmail) : IEvent
{
    public int DocumentId { get; } = documentId;
    public string RecipientEmail { get; } = recipientEmail;
}

/// <summary>Published when the print queue is processed.</summary>
public sealed class PrintQueueProcessedEvent(int itemCount) : IEvent
{
    public int ItemCount { get; } = itemCount;
}
