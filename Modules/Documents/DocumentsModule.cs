using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Documents.Services;

namespace StoreAssistantPro.Modules.Documents;

public static class DocumentsModule
{
    public static IServiceCollection AddDocumentsModule(this IServiceCollection services)
    {
        // DB-accessing services with no mutable state → Transient.
        services.AddTransient<IPdfGenerationService, PdfGenerationService>();
        services.AddTransient<IDocumentStorageService, DocumentStorageService>();
        services.AddTransient<IDocumentTemplateService, DocumentTemplateService>();
        services.AddTransient<IDocumentEmailService, DocumentEmailService>();

        // Holds print queue state → Singleton.
        services.AddSingleton<IPrintQueueService, PrintQueueService>();
        return services;
    }
}
