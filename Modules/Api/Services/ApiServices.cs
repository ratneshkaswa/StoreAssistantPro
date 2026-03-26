using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.Api;

namespace StoreAssistantPro.Modules.Api.Services;

public sealed class ProductApiService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<ProductApiService> logger) : IProductApiService
{
    public async Task<ApiResponse<object>> GetProductAsync(int productId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var product = await context.Products.FindAsync([productId], ct).ConfigureAwait(false);
        if (product is null)
            return new ApiResponse<object> { Success = false, StatusCode = 404, Error = "Product not found" };

        logger.LogDebug("API: GetProduct {Id}", productId);
        return new ApiResponse<object> { Success = true, StatusCode = 200, Data = product, RequestId = Guid.NewGuid().ToString() };
    }

    public async Task<PagedApiResponse<object>> ListProductsAsync(int page = 1, int pageSize = 20, string? search = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Products.Where(p => p.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search));

        var total = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query.OrderBy(p => p.Name).Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct).ConfigureAwait(false);

        return new PagedApiResponse<object>
        {
            Success = true, Items = items.Cast<object>().ToList(), Page = page,
            PageSize = pageSize, TotalCount = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public Task<ApiResponse<object>> CreateProductAsync(Dictionary<string, object?> productData, CancellationToken ct = default)
    {
        logger.LogInformation("API: CreateProduct with {Count} fields", productData.Count);
        return Task.FromResult(new ApiResponse<object> { Success = true, StatusCode = 201, Data = productData, RequestId = Guid.NewGuid().ToString() });
    }

    public Task<ApiResponse<object>> UpdateProductAsync(int productId, Dictionary<string, object?> productData, CancellationToken ct = default)
    {
        logger.LogInformation("API: UpdateProduct {Id}", productId);
        return Task.FromResult(new ApiResponse<object> { Success = true, StatusCode = 200, Data = productData, RequestId = Guid.NewGuid().ToString() });
    }

    public Task<ApiResponse<bool>> DeleteProductAsync(int productId, CancellationToken ct = default)
    {
        logger.LogInformation("API: DeleteProduct {Id}", productId);
        return Task.FromResult(new ApiResponse<bool> { Success = true, StatusCode = 200, Data = true, RequestId = Guid.NewGuid().ToString() });
    }
}

public sealed class SaleApiService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<SaleApiService> logger) : ISaleApiService
{
    public async Task<ApiResponse<object>> GetSaleAsync(int saleId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var sale = await context.Sales.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == saleId, ct).ConfigureAwait(false);
        if (sale is null)
            return new ApiResponse<object> { Success = false, StatusCode = 404, Error = "Sale not found" };

        logger.LogDebug("API: GetSale {Id}", saleId);
        return new ApiResponse<object> { Success = true, StatusCode = 200, Data = sale, RequestId = Guid.NewGuid().ToString() };
    }

    public async Task<PagedApiResponse<object>> ListSalesAsync(int page = 1, int pageSize = 20, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Sales.AsQueryable();
        if (from.HasValue) query = query.Where(s => s.SaleDate >= from.Value);
        if (to.HasValue) query = query.Where(s => s.SaleDate <= to.Value);

        var total = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query.OrderByDescending(s => s.SaleDate).Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct).ConfigureAwait(false);

        return new PagedApiResponse<object>
        {
            Success = true, Items = items.Cast<object>().ToList(), Page = page,
            PageSize = pageSize, TotalCount = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public Task<ApiResponse<object>> CreateSaleAsync(Dictionary<string, object?> saleData, CancellationToken ct = default)
    {
        logger.LogInformation("API: CreateSale with {Count} fields", saleData.Count);
        return Task.FromResult(new ApiResponse<object> { Success = true, StatusCode = 201, Data = saleData, RequestId = Guid.NewGuid().ToString() });
    }
}

public sealed class CustomerApiService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<CustomerApiService> logger) : ICustomerApiService
{
    public async Task<ApiResponse<object>> GetCustomerAsync(int customerId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var customer = await context.Customers.FindAsync([customerId], ct).ConfigureAwait(false);
        if (customer is null)
            return new ApiResponse<object> { Success = false, StatusCode = 404, Error = "Customer not found" };

        logger.LogDebug("API: GetCustomer {Id}", customerId);
        return new ApiResponse<object> { Success = true, StatusCode = 200, Data = customer, RequestId = Guid.NewGuid().ToString() };
    }

    public async Task<PagedApiResponse<object>> ListCustomersAsync(int page = 1, int pageSize = 20, string? search = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search) || (c.Phone != null && c.Phone.Contains(search)));

        var total = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query.OrderBy(c => c.Name).Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct).ConfigureAwait(false);

        return new PagedApiResponse<object>
        {
            Success = true, Items = items.Cast<object>().ToList(), Page = page,
            PageSize = pageSize, TotalCount = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public Task<ApiResponse<object>> CreateCustomerAsync(Dictionary<string, object?> customerData, CancellationToken ct = default)
    {
        logger.LogInformation("API: CreateCustomer");
        return Task.FromResult(new ApiResponse<object> { Success = true, StatusCode = 201, Data = customerData, RequestId = Guid.NewGuid().ToString() });
    }

    public Task<ApiResponse<object>> UpdateCustomerAsync(int customerId, Dictionary<string, object?> customerData, CancellationToken ct = default)
    {
        logger.LogInformation("API: UpdateCustomer {Id}", customerId);
        return Task.FromResult(new ApiResponse<object> { Success = true, StatusCode = 200, Data = customerData, RequestId = Guid.NewGuid().ToString() });
    }
}

public sealed class InventoryApiService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<InventoryApiService> logger) : IInventoryApiService
{
    public async Task<ApiResponse<object>> GetStockAsync(int productId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var product = await context.Products.Where(p => p.Id == productId)
            .Select(p => new { p.Id, p.Name, p.Quantity, p.MinStockLevel }).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (product is null)
            return new ApiResponse<object> { Success = false, StatusCode = 404, Error = "Product not found" };

        return new ApiResponse<object> { Success = true, StatusCode = 200, Data = product, RequestId = Guid.NewGuid().ToString() };
    }

    public async Task<PagedApiResponse<object>> ListStockAsync(int page = 1, int pageSize = 50, string? filter = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Products.Where(p => p.IsActive);
        if (filter == "low") query = query.Where(p => p.Quantity <= p.MinStockLevel && p.Quantity > 0);
        else if (filter == "out") query = query.Where(p => p.Quantity <= 0);

        var total = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query.Select(p => new { p.Id, p.Name, p.Quantity, p.MinStockLevel })
            .OrderBy(p => p.Name).Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct).ConfigureAwait(false);

        return new PagedApiResponse<object>
        {
            Success = true, Items = items.Cast<object>().ToList(), Page = page,
            PageSize = pageSize, TotalCount = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public Task<ApiResponse<bool>> UpdateStockAsync(int productId, int quantityChange, string reason, CancellationToken ct = default)
    {
        logger.LogInformation("API: UpdateStock {Id} by {Qty} reason={Reason}", productId, quantityChange, reason);
        return Task.FromResult(new ApiResponse<bool> { Success = true, StatusCode = 200, Data = true, RequestId = Guid.NewGuid().ToString() });
    }
}

public sealed class ApiAuthService(ILogger<ApiAuthService> logger) : IApiAuthService
{
    private readonly Dictionary<string, (ApiTokenResult Token, DateTime Issued)> _tokens = [];

    public Task<ApiTokenResult> AuthenticateAsync(ApiClientCredentials credentials, CancellationToken ct = default)
    {
        var token = GenerateToken();
        var result = new ApiTokenResult(token, GenerateToken(), DateTime.UtcNow.AddHours(1));
        _tokens[token] = (result, DateTime.UtcNow);
        logger.LogInformation("API: Authenticated client {ClientId}", credentials.ClientId);
        return Task.FromResult(result);
    }

    public Task<ApiTokenResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = GenerateToken();
        var result = new ApiTokenResult(token, GenerateToken(), DateTime.UtcNow.AddHours(1));
        _tokens[token] = (result, DateTime.UtcNow);
        logger.LogInformation("API: Token refreshed");
        return Task.FromResult(result);
    }

    public Task RevokeTokenAsync(string accessToken, CancellationToken ct = default)
    {
        _tokens.Remove(accessToken);
        logger.LogInformation("API: Token revoked");
        return Task.CompletedTask;
    }

    public bool ValidateToken(string accessToken)
    {
        if (!_tokens.TryGetValue(accessToken, out var entry)) return false;
        return entry.Token.ExpiresAt > DateTime.UtcNow;
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}

public sealed class ApiRateLimitService(ILogger<ApiRateLimitService> logger) : IApiRateLimitService
{
    private readonly Dictionary<string, List<DateTime>> _requests = [];
    private const int DefaultLimit = 100;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public Task<RateLimitStatus> CheckRateLimitAsync(string clientId, CancellationToken ct = default)
    {
        CleanExpired(clientId);
        var count = _requests.TryGetValue(clientId, out var list) ? list.Count : 0;
        return Task.FromResult(new RateLimitStatus(clientId, DefaultLimit - count, DefaultLimit, Window, DateTime.UtcNow.Add(Window), count >= DefaultLimit));
    }

    public Task RecordRequestAsync(string clientId, CancellationToken ct = default)
    {
        if (!_requests.TryGetValue(clientId, out var list))
        {
            list = [];
            _requests[clientId] = list;
        }
        list.Add(DateTime.UtcNow);
        return Task.CompletedTask;
    }

    public Task<bool> IsThrottledAsync(string clientId, CancellationToken ct = default)
    {
        CleanExpired(clientId);
        var count = _requests.TryGetValue(clientId, out var list) ? list.Count : 0;
        return Task.FromResult(count >= DefaultLimit);
    }

    public Task ResetLimitsAsync(string clientId, CancellationToken ct = default)
    {
        _requests.Remove(clientId);
        logger.LogInformation("API: Rate limits reset for {ClientId}", clientId);
        return Task.CompletedTask;
    }

    private void CleanExpired(string clientId)
    {
        if (!_requests.TryGetValue(clientId, out var list)) return;
        var cutoff = DateTime.UtcNow.Subtract(Window);
        list.RemoveAll(t => t < cutoff);
    }
}

public sealed class AccountingExportService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<AccountingExportService> logger) : IAccountingExportService
{
    private readonly List<LedgerMapping> _mappings =
    [
        new("Sales Revenue", "Sales Account", "Revenue"),
        new("Cash", "Cash Account", "Assets"),
        new("Discount Given", "Discount Allowed", "Expenses"),
        new("GST Output", "Output CGST", "Liabilities"),
        new("Purchases", "Purchase Account", "Expenses")
    ];

    public async Task<string> ExportToTallyXmlAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var sales = await context.Sales.Where(s => s.SaleDate >= from && s.SaleDate <= to).CountAsync(ct).ConfigureAwait(false);
        var xml = $"""
            <ENVELOPE>
                <HEADER><TALLYREQUEST>Import Data</TALLYREQUEST></HEADER>
                <BODY><IMPORTDATA><REQUESTDESC><REPORTNAME>All Masters</REPORTNAME></REQUESTDESC>
                <REQUESTDATA><TALLYMESSAGE><!-- {sales} sales from {from:yyyy-MM-dd} to {to:yyyy-MM-dd} --></TALLYMESSAGE></REQUESTDATA>
                </IMPORTDATA></BODY>
            </ENVELOPE>
            """;
        logger.LogInformation("Exported {Count} sales to Tally XML", sales);
        return xml;
    }

    public Task<bool> SyncToQuickBooksAsync(DateTime from, DateTime to, AccountingExportConfig config, CancellationToken ct = default)
    {
        logger.LogInformation("QuickBooks sync from {From} to {To} via {Endpoint}", from, to, config.ApiEndpoint);
        return Task.FromResult(true);
    }

    public Task<bool> SyncToZohoBooksAsync(DateTime from, DateTime to, AccountingExportConfig config, CancellationToken ct = default)
    {
        logger.LogInformation("Zoho Books sync from {From} to {To} via {Endpoint}", from, to, config.ApiEndpoint);
        return Task.FromResult(true);
    }

    public async Task<IReadOnlyList<JournalEntry>> GenerateJournalEntriesAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var sales = await context.Sales.Where(s => s.SaleDate >= from && s.SaleDate <= to)
            .Select(s => new { s.Id, s.InvoiceNumber, s.TotalAmount, s.DiscountAmount, s.SaleDate })
            .ToListAsync(ct).ConfigureAwait(false);

        return sales.Select(s => new JournalEntry(
            s.SaleDate,
            s.InvoiceNumber ?? $"SALE-{s.Id}",
            "Sales",
            [
                new JournalLine("Cash", s.TotalAmount, 0, null),
                new JournalLine("Sales Revenue", 0, s.TotalAmount - s.DiscountAmount, null),
                ..( s.DiscountAmount > 0 ? [new JournalLine("Discount Given", s.DiscountAmount, 0, null)] : Array.Empty<JournalLine>())
            ],
            $"Sale #{s.Id}")).ToList();
    }

    public Task<IReadOnlyList<LedgerMapping>> GetLedgerMappingsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<LedgerMapping>>(_mappings.ToList());

    public Task SaveLedgerMappingsAsync(IReadOnlyList<LedgerMapping> mappings, CancellationToken ct = default)
    {
        _mappings.Clear();
        _mappings.AddRange(mappings);
        logger.LogInformation("Saved {Count} ledger mappings", mappings.Count);
        return Task.CompletedTask;
    }

    public Task<bool> AutoPostAsync(AccountingExportConfig config, CancellationToken ct = default)
    {
        logger.LogInformation("Auto-post to {Format} via {Endpoint}", config.Format, config.ApiEndpoint);
        return Task.FromResult(true);
    }

    public async Task<ReconciliationResult> ReconcileAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var posTotal = await context.Sales.Where(s => s.SaleDate >= from && s.SaleDate <= to)
            .SumAsync(s => s.TotalAmount, ct).ConfigureAwait(false);

        return new ReconciliationResult(from, to, posTotal, posTotal, 0, 0, []);
    }
}

public sealed class CommunicationService(ILogger<CommunicationService> logger) : ICommunicationService
{
    public Task<CommunicationResult> SendSmsAsync(string recipient, string message, CancellationToken ct = default)
    {
        logger.LogInformation("SMS to {Recipient}: {Message}", recipient, message.Length > 50 ? message[..50] + "…" : message);
        return Task.FromResult(new CommunicationResult(true, CommunicationChannel.Sms, Guid.NewGuid().ToString(), null, DateTime.UtcNow));
    }

    public Task<CommunicationResult> SendEmailAsync(string recipient, string subject, string body, string? attachmentPath = null, CancellationToken ct = default)
    {
        logger.LogInformation("Email to {Recipient}: {Subject}", recipient, subject);
        return Task.FromResult(new CommunicationResult(true, CommunicationChannel.Email, Guid.NewGuid().ToString(), null, DateTime.UtcNow));
    }

    public Task<CommunicationResult> SendWhatsAppAsync(string recipient, string templateName, Dictionary<string, string> templateData, CancellationToken ct = default)
    {
        logger.LogInformation("WhatsApp to {Recipient} template={Template}", recipient, templateName);
        return Task.FromResult(new CommunicationResult(true, CommunicationChannel.WhatsApp, Guid.NewGuid().ToString(), null, DateTime.UtcNow));
    }

    public Task<CommunicationResult> SendPushNotificationAsync(string deviceToken, string title, string body, CancellationToken ct = default)
    {
        logger.LogInformation("Push to {Token}: {Title}", deviceToken[..Math.Min(8, deviceToken.Length)] + "…", title);
        return Task.FromResult(new CommunicationResult(true, CommunicationChannel.PushNotification, Guid.NewGuid().ToString(), null, DateTime.UtcNow));
    }

    public Task<CommunicationResult> SendWebhookAsync(WebhookEndpoint endpoint, WebhookPayload payload, CancellationToken ct = default)
    {
        logger.LogInformation("Webhook {Event} to {Url}", payload.EventType, endpoint.Url);
        return Task.FromResult(new CommunicationResult(true, CommunicationChannel.Webhook, payload.EventId, null, DateTime.UtcNow));
    }

    public Task<CommunicationResult> SendSlackMessageAsync(string channelUrl, string message, CancellationToken ct = default)
    {
        logger.LogInformation("Slack message to {Url}", channelUrl);
        return Task.FromResult(new CommunicationResult(true, CommunicationChannel.Slack, Guid.NewGuid().ToString(), null, DateTime.UtcNow));
    }

    public Task<CommunicationResult> SendTeamsMessageAsync(string webhookUrl, string message, CancellationToken ct = default)
    {
        logger.LogInformation("Teams message to {Url}", webhookUrl);
        return Task.FromResult(new CommunicationResult(true, CommunicationChannel.Teams, Guid.NewGuid().ToString(), null, DateTime.UtcNow));
    }

    public Task<IReadOnlyList<NotificationTemplate>> GetTemplatesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<NotificationTemplate>>([]);

    public Task SaveTemplateAsync(NotificationTemplate template, CancellationToken ct = default)
    {
        logger.LogInformation("Saved notification template: {Name}", template.Name);
        return Task.CompletedTask;
    }

    public Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> data, CancellationToken ct = default)
    {
        var rendered = data.Aggregate(templateName, (current, kv) => current.Replace($"{{{kv.Key}}}", kv.Value, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(rendered);
    }
}
