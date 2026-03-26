using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.Documents;

namespace StoreAssistantPro.Modules.Documents.Services;

public sealed class PdfGenerationService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<PdfGenerationService> logger) : IPdfGenerationService
{
    public async Task<string> GenerateInvoicePdfAsync(int saleId, string? templateName = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var sale = await context.Sales.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == saleId, ct).ConfigureAwait(false);
        if (sale is null)
            throw new InvalidOperationException($"Sale {saleId} not found");

        var dir = GetOutputDirectory();
        var path = Path.Combine(dir, $"Invoice_{sale.InvoiceNumber ?? saleId.ToString()}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");

        // Placeholder: actual PDF generation would use a library like QuestPDF.
        await File.WriteAllTextAsync(path, $"[PDF INVOICE] Sale #{saleId}, Total: {sale.TotalAmount:N2}", ct).ConfigureAwait(false);
        logger.LogInformation("Generated invoice PDF for sale {SaleId}: {Path}", saleId, path);
        return path;
    }

    public async Task<string> GenerateReceiptPdfAsync(int saleId, string? templateName = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var sale = await context.Sales.FirstOrDefaultAsync(s => s.Id == saleId, ct).ConfigureAwait(false);
        if (sale is null)
            throw new InvalidOperationException($"Sale {saleId} not found");

        var dir = GetOutputDirectory();
        var path = Path.Combine(dir, $"Receipt_{saleId}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
        await File.WriteAllTextAsync(path, $"[PDF RECEIPT] Sale #{saleId}, Total: {sale.TotalAmount:N2}", ct).ConfigureAwait(false);
        logger.LogInformation("Generated receipt PDF for sale {SaleId}: {Path}", saleId, path);
        return path;
    }

    public async Task<string> GenerateReportPdfAsync(string reportType, DateTime from, DateTime to, string? templateName = null, CancellationToken ct = default)
    {
        var dir = GetOutputDirectory();
        var path = Path.Combine(dir, $"Report_{reportType}_{from:yyyyMMdd}_{to:yyyyMMdd}.pdf");
        await File.WriteAllTextAsync(path, $"[PDF REPORT] {reportType} from {from:yyyy-MM-dd} to {to:yyyy-MM-dd}", ct).ConfigureAwait(false);
        logger.LogInformation("Generated {ReportType} report PDF: {Path}", reportType, path);
        return path;
    }

    public async Task<BatchDocumentResult> GenerateBatchAsync(BatchDocumentRequest request, CancellationToken ct = default)
    {
        var generated = new List<string>();
        var errors = new List<string>();

        foreach (var entityId in request.EntityIds)
        {
            try
            {
                var path = request.DocumentType switch
                {
                    "Invoice" => await GenerateInvoicePdfAsync(entityId, request.TemplateName, ct).ConfigureAwait(false),
                    "Receipt" => await GenerateReceiptPdfAsync(entityId, request.TemplateName, ct).ConfigureAwait(false),
                    _ => throw new InvalidOperationException($"Unknown document type: {request.DocumentType}")
                };
                generated.Add(path);
            }
            catch (Exception ex)
            {
                errors.Add($"Entity {entityId}: {ex.Message}");
            }
        }

        logger.LogInformation("Batch generation: {Success}/{Total} succeeded", generated.Count, request.EntityIds.Count);
        return new BatchDocumentResult(request.EntityIds.Count, generated.Count, errors.Count, generated, errors);
    }

    private static string GetOutputDirectory()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoreAssistantPro", "Documents");
        Directory.CreateDirectory(dir);
        return dir;
    }
}

public sealed class DocumentStorageService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<DocumentStorageService> logger) : IDocumentStorageService
{
    public async Task<StoredDocument> StoreDocumentAsync(string filePath, string documentType,
        int? relatedEntityId = null, string? relatedEntityType = null, int? customerId = null,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var fileInfo = new FileInfo(filePath);

        var doc = new StoredDocument
        {
            DocumentType = documentType,
            FileName = fileInfo.Name,
            FilePath = filePath,
            FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            CustomerId = customerId,
            CreatedAt = DateTime.UtcNow
        };

        context.StoredDocuments.Add(doc);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Stored document: {FileName} ({Type})", doc.FileName, documentType);
        return doc;
    }

    public async Task<IReadOnlyList<StoredDocument>> SearchDocumentsAsync(string? documentType = null,
        DateTime? from = null, DateTime? to = null, int? customerId = null, string? searchText = null,
        CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.StoredDocuments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(documentType)) query = query.Where(d => d.DocumentType == documentType);
        if (from.HasValue) query = query.Where(d => d.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(d => d.CreatedAt <= to.Value);
        if (customerId.HasValue) query = query.Where(d => d.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(searchText))
            query = query.Where(d => d.FileName.Contains(searchText) || (d.Tags != null && d.Tags.Contains(searchText)));

        return await query.OrderByDescending(d => d.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<StoredDocument?> GetDocumentAsync(int documentId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.StoredDocuments.FindAsync([documentId], ct).ConfigureAwait(false);
    }

    public async Task DeleteDocumentAsync(int documentId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var doc = await context.StoredDocuments.FindAsync([documentId], ct).ConfigureAwait(false);
        if (doc is null) return;

        context.StoredDocuments.Remove(doc);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        if (File.Exists(doc.FilePath))
            File.Delete(doc.FilePath);

        logger.LogInformation("Deleted document {Id}: {FileName}", documentId, doc.FileName);
    }

    public string GetStorageDirectory()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoreAssistantPro", "Documents");
        Directory.CreateDirectory(dir);
        return dir;
    }
}

public sealed class DocumentEmailService(ILogger<DocumentEmailService> logger) : IDocumentEmailService
{
    public Task<bool> EmailDocumentAsync(int documentId, string recipientEmail, string? subject = null,
        string? body = null, CancellationToken ct = default)
    {
        logger.LogInformation("Email document {Id} to {Email}", documentId, recipientEmail);
        return Task.FromResult(true);
    }

    public Task<bool> EmailFileAsync(string filePath, string recipientEmail, string subject,
        string? body = null, CancellationToken ct = default)
    {
        logger.LogInformation("Email file {File} to {Email}: {Subject}", Path.GetFileName(filePath), recipientEmail, subject);
        return Task.FromResult(true);
    }
}

public sealed class PrintQueueService(ILogger<PrintQueueService> logger) : IPrintQueueService
{
    private readonly List<PrintQueueItem> _queue = [];

    public Task<PrintQueueItem> EnqueueAsync(string documentPath, string documentType, string? printerName = null,
        int copies = 1, CancellationToken ct = default)
    {
        var item = new PrintQueueItem
        {
            DocumentPath = documentPath,
            DocumentType = documentType,
            PrinterName = printerName,
            Copies = copies,
            QueuedAt = DateTime.UtcNow
        };
        _queue.Add(item);
        logger.LogInformation("Enqueued document for printing: {Path} ({Copies} copies)", documentPath, copies);
        return Task.FromResult(item);
    }

    public IReadOnlyList<PrintQueueItem> GetQueue() => _queue.ToList();

    public Task<int> ProcessQueueAsync(CancellationToken ct = default)
    {
        var count = _queue.Count;
        foreach (var item in _queue) item.Status = "Completed";
        logger.LogInformation("Processed {Count} print queue items", count);
        _queue.Clear();
        return Task.FromResult(count);
    }

    public void ClearQueue()
    {
        _queue.Clear();
        logger.LogInformation("Print queue cleared");
    }
}

public sealed class DocumentTemplateService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<DocumentTemplateService> logger) : IDocumentTemplateService
{
    public async Task<IReadOnlyList<DocumentTemplate>> GetTemplatesAsync(string? documentType = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.DocumentTemplates.Where(t => t.IsActive);
        if (!string.IsNullOrWhiteSpace(documentType)) query = query.Where(t => t.DocumentType == documentType);
        return await query.OrderBy(t => t.Name).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<DocumentTemplate?> GetDefaultTemplateAsync(string documentType, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.DocumentTemplates
            .FirstOrDefaultAsync(t => t.DocumentType == documentType && t.IsDefault && t.IsActive, ct)
            .ConfigureAwait(false);
    }

    public async Task<DocumentTemplate> SaveTemplateAsync(DocumentTemplate template, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        if (template.Id == 0)
        {
            template.CreatedAt = DateTime.UtcNow;
            context.DocumentTemplates.Add(template);
        }
        else
        {
            context.DocumentTemplates.Update(template);
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Saved document template: {Name} ({Type})", template.Name, template.DocumentType);
        return template;
    }

    public async Task DeleteTemplateAsync(int templateId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var template = await context.DocumentTemplates.FindAsync([templateId], ct).ConfigureAwait(false);
        if (template is null) return;

        template.IsActive = false;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Deleted document template {Id}", templateId);
    }

    public async Task SetDefaultTemplateAsync(int templateId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var template = await context.DocumentTemplates.FindAsync([templateId], ct).ConfigureAwait(false);
        if (template is null) return;

        // Clear existing default for this type.
        var existing = await context.DocumentTemplates
            .Where(t => t.DocumentType == template.DocumentType && t.IsDefault)
            .ToListAsync(ct).ConfigureAwait(false);
        foreach (var t in existing) t.IsDefault = false;

        template.IsDefault = true;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Set template {Id} as default for {Type}", templateId, template.DocumentType);
    }
}
