namespace StoreAssistantPro.Models.Documents;

/// <summary>
/// Document storage record tracking generated documents (#884).
/// </summary>
public sealed class StoredDocument
{
    public int Id { get; set; }
    public string DocumentType { get; set; } = string.Empty; // Invoice, Receipt, Report
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; } // Sale, Purchase, Expense, Customer
    public int? CustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByUser { get; set; }
    public string? Tags { get; set; }
}

/// <summary>
/// Document template configuration (#888).
/// </summary>
public sealed class DocumentTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty; // Invoice, Receipt, Report
    public string? HeaderHtml { get; set; }
    public string? FooterHtml { get; set; }
    public string? BodyTemplate { get; set; }
    public string? LogoPath { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Document print queue item (#887).
/// </summary>
public sealed class PrintQueueItem
{
    public string DocumentPath { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string? PrinterName { get; set; }
    public int Copies { get; set; } = 1;
    public string Status { get; set; } = "Queued"; // Queued, Printing, Completed, Failed
    public DateTime QueuedAt { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Batch document generation request (#889).
/// </summary>
public sealed class BatchDocumentRequest
{
    public string DocumentType { get; set; } = string.Empty;
    public IReadOnlyList<int> EntityIds { get; set; } = [];
    public string? TemplateName { get; set; }
    public string OutputDirectory { get; set; } = string.Empty;
}

/// <summary>
/// Result of a batch document generation.
/// </summary>
public sealed record BatchDocumentResult(
    int TotalRequested,
    int SuccessCount,
    int FailedCount,
    IReadOnlyList<string> GeneratedFiles,
    IReadOnlyList<string> Errors);
