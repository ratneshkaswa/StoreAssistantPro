using StoreAssistantPro.Models.Api;

namespace StoreAssistantPro.Modules.Api.Services;

/// <summary>
/// Product REST API service (#686).
/// CRUD products via HTTP-compatible interface.
/// </summary>
public interface IProductApiService
{
    Task<ApiResponse<object>> GetProductAsync(int productId, CancellationToken ct = default);
    Task<PagedApiResponse<object>> ListProductsAsync(int page = 1, int pageSize = 20, string? search = null, CancellationToken ct = default);
    Task<ApiResponse<object>> CreateProductAsync(Dictionary<string, object?> productData, CancellationToken ct = default);
    Task<ApiResponse<object>> UpdateProductAsync(int productId, Dictionary<string, object?> productData, CancellationToken ct = default);
    Task<ApiResponse<bool>> DeleteProductAsync(int productId, CancellationToken ct = default);
}

/// <summary>
/// Sale REST API service (#687).
/// Create and read sales via API.
/// </summary>
public interface ISaleApiService
{
    Task<ApiResponse<object>> GetSaleAsync(int saleId, CancellationToken ct = default);
    Task<PagedApiResponse<object>> ListSalesAsync(int page = 1, int pageSize = 20, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<ApiResponse<object>> CreateSaleAsync(Dictionary<string, object?> saleData, CancellationToken ct = default);
}

/// <summary>
/// Customer REST API service (#688).
/// CRUD customers via API.
/// </summary>
public interface ICustomerApiService
{
    Task<ApiResponse<object>> GetCustomerAsync(int customerId, CancellationToken ct = default);
    Task<PagedApiResponse<object>> ListCustomersAsync(int page = 1, int pageSize = 20, string? search = null, CancellationToken ct = default);
    Task<ApiResponse<object>> CreateCustomerAsync(Dictionary<string, object?> customerData, CancellationToken ct = default);
    Task<ApiResponse<object>> UpdateCustomerAsync(int customerId, Dictionary<string, object?> customerData, CancellationToken ct = default);
}

/// <summary>
/// Inventory REST API service (#689).
/// Read and update stock via API.
/// </summary>
public interface IInventoryApiService
{
    Task<ApiResponse<object>> GetStockAsync(int productId, CancellationToken ct = default);
    Task<PagedApiResponse<object>> ListStockAsync(int page = 1, int pageSize = 50, string? filter = null, CancellationToken ct = default);
    Task<ApiResponse<bool>> UpdateStockAsync(int productId, int quantityChange, string reason, CancellationToken ct = default);
}

/// <summary>
/// API authentication using JWT (#690).
/// </summary>
public interface IApiAuthService
{
    Task<ApiTokenResult> AuthenticateAsync(ApiClientCredentials credentials, CancellationToken ct = default);
    Task<ApiTokenResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeTokenAsync(string accessToken, CancellationToken ct = default);
    bool ValidateToken(string accessToken);
}

/// <summary>
/// API rate limiting service (#691).
/// </summary>
public interface IApiRateLimitService
{
    Task<RateLimitStatus> CheckRateLimitAsync(string clientId, CancellationToken ct = default);
    Task RecordRequestAsync(string clientId, CancellationToken ct = default);
    Task<bool> IsThrottledAsync(string clientId, CancellationToken ct = default);
    Task ResetLimitsAsync(string clientId, CancellationToken ct = default);
}

/// <summary>
/// Accounting export service covering Tally, QuickBooks, Zoho, journal entries,
/// ledger mapping, auto-posting, and reconciliation (#693-699).
/// </summary>
public interface IAccountingExportService
{
    Task<string> ExportToTallyXmlAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<bool> SyncToQuickBooksAsync(DateTime from, DateTime to, AccountingExportConfig config, CancellationToken ct = default);
    Task<bool> SyncToZohoBooksAsync(DateTime from, DateTime to, AccountingExportConfig config, CancellationToken ct = default);
    Task<IReadOnlyList<JournalEntry>> GenerateJournalEntriesAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<LedgerMapping>> GetLedgerMappingsAsync(CancellationToken ct = default);
    Task SaveLedgerMappingsAsync(IReadOnlyList<LedgerMapping> mappings, CancellationToken ct = default);
    Task<bool> AutoPostAsync(AccountingExportConfig config, CancellationToken ct = default);
    Task<ReconciliationResult> ReconcileAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

/// <summary>
/// Communication API service for SMS, Email, WhatsApp, Push, Webhooks,
/// Slack/Teams, and notification templates (#700-706).
/// </summary>
public interface ICommunicationService
{
    Task<CommunicationResult> SendSmsAsync(string recipient, string message, CancellationToken ct = default);
    Task<CommunicationResult> SendEmailAsync(string recipient, string subject, string body, string? attachmentPath = null, CancellationToken ct = default);
    Task<CommunicationResult> SendWhatsAppAsync(string recipient, string templateName, Dictionary<string, string> templateData, CancellationToken ct = default);
    Task<CommunicationResult> SendPushNotificationAsync(string deviceToken, string title, string body, CancellationToken ct = default);
    Task<CommunicationResult> SendWebhookAsync(WebhookEndpoint endpoint, WebhookPayload payload, CancellationToken ct = default);
    Task<CommunicationResult> SendSlackMessageAsync(string channelUrl, string message, CancellationToken ct = default);
    Task<CommunicationResult> SendTeamsMessageAsync(string webhookUrl, string message, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationTemplate>> GetTemplatesAsync(CancellationToken ct = default);
    Task SaveTemplateAsync(NotificationTemplate template, CancellationToken ct = default);
    Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> data, CancellationToken ct = default);
}
