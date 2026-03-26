using StoreAssistantPro.Models.Documents;

namespace StoreAssistantPro.Modules.Documents.Services;

/// <summary>
/// PDF generation service for invoices, receipts, and reports (#881-883).
/// </summary>
public interface IPdfGenerationService
{
    /// <summary>Generate a PDF invoice for a sale (#881).</summary>
    Task<string> GenerateInvoicePdfAsync(int saleId, string? templateName = null, CancellationToken ct = default);

    /// <summary>Generate a PDF receipt for a sale (#882).</summary>
    Task<string> GenerateReceiptPdfAsync(int saleId, string? templateName = null, CancellationToken ct = default);

    /// <summary>Generate a PDF report (#883).</summary>
    Task<string> GenerateReportPdfAsync(string reportType, DateTime from, DateTime to, string? templateName = null, CancellationToken ct = default);

    /// <summary>Generate batch documents (#889).</summary>
    Task<BatchDocumentResult> GenerateBatchAsync(BatchDocumentRequest request, CancellationToken ct = default);
}

/// <summary>
/// Document storage and search service (#884, #885).
/// </summary>
public interface IDocumentStorageService
{
    /// <summary>Store a generated document (#884).</summary>
    Task<StoredDocument> StoreDocumentAsync(string filePath, string documentType, int? relatedEntityId = null, string? relatedEntityType = null, int? customerId = null, CancellationToken ct = default);

    /// <summary>Search stored documents (#885).</summary>
    Task<IReadOnlyList<StoredDocument>> SearchDocumentsAsync(string? documentType = null, DateTime? from = null, DateTime? to = null, int? customerId = null, string? searchText = null, CancellationToken ct = default);

    /// <summary>Get a stored document by ID.</summary>
    Task<StoredDocument?> GetDocumentAsync(int documentId, CancellationToken ct = default);

    /// <summary>Delete a stored document.</summary>
    Task DeleteDocumentAsync(int documentId, CancellationToken ct = default);

    /// <summary>Get storage directory path.</summary>
    string GetStorageDirectory();
}

/// <summary>
/// Document email service (#886).
/// </summary>
public interface IDocumentEmailService
{
    /// <summary>Email a stored document (#886).</summary>
    Task<bool> EmailDocumentAsync(int documentId, string recipientEmail, string? subject = null, string? body = null, CancellationToken ct = default);

    /// <summary>Email a document directly by file path.</summary>
    Task<bool> EmailFileAsync(string filePath, string recipientEmail, string subject, string? body = null, CancellationToken ct = default);
}

/// <summary>
/// Document print queue service (#887).
/// </summary>
public interface IPrintQueueService
{
    /// <summary>Add a document to the print queue (#887).</summary>
    Task<PrintQueueItem> EnqueueAsync(string documentPath, string documentType, string? printerName = null, int copies = 1, CancellationToken ct = default);

    /// <summary>Get all items in the print queue.</summary>
    IReadOnlyList<PrintQueueItem> GetQueue();

    /// <summary>Process the print queue (print all queued items).</summary>
    Task<int> ProcessQueueAsync(CancellationToken ct = default);

    /// <summary>Clear the print queue.</summary>
    void ClearQueue();
}

/// <summary>
/// Document template editor service (#888).
/// </summary>
public interface IDocumentTemplateService
{
    /// <summary>Get all document templates (#888).</summary>
    Task<IReadOnlyList<DocumentTemplate>> GetTemplatesAsync(string? documentType = null, CancellationToken ct = default);

    /// <summary>Get the default template for a document type.</summary>
    Task<DocumentTemplate?> GetDefaultTemplateAsync(string documentType, CancellationToken ct = default);

    /// <summary>Save a document template.</summary>
    Task<DocumentTemplate> SaveTemplateAsync(DocumentTemplate template, CancellationToken ct = default);

    /// <summary>Delete a document template.</summary>
    Task DeleteTemplateAsync(int templateId, CancellationToken ct = default);

    /// <summary>Set a template as the default for its document type.</summary>
    Task SetDefaultTemplateAsync(int templateId, CancellationToken ct = default);
}
