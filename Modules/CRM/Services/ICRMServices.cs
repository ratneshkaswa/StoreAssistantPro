using StoreAssistantPro.Models.CRM;

namespace StoreAssistantPro.Modules.CRM.Services;

/// <summary>Campaign management service (#799-808).</summary>
public interface ICampaignService
{
    Task<IReadOnlyList<Campaign>> GetCampaignsAsync(string? status = null, CancellationToken ct = default);
    Task<Campaign> SaveCampaignAsync(Campaign campaign, CancellationToken ct = default);
    Task<Campaign> SendCampaignAsync(int campaignId, CancellationToken ct = default);
    Task ScheduleCampaignAsync(int campaignId, DateTime scheduledAt, CancellationToken ct = default);
    Task<Campaign> GetCampaignAnalyticsAsync(int campaignId, CancellationToken ct = default);
}

/// <summary>Auto-greeting service (#801-802).</summary>
public interface IAutoGreetingService
{
    Task SendBirthdayGreetingsAsync(CancellationToken ct = default);
    Task SendAnniversaryOffersAsync(CancellationToken ct = default);
}

/// <summary>Feedback and NPS service (#804-805).</summary>
public interface IFeedbackService
{
    Task<CustomerFeedback> RecordFeedbackAsync(CustomerFeedback feedback, CancellationToken ct = default);
    Task<double> CalculateNpsAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerFeedback>> GetFeedbackAsync(int? customerId = null, CancellationToken ct = default);
}

/// <summary>Service ticket management (#809-818).</summary>
public interface IServiceTicketService
{
    Task<ServiceTicket> CreateTicketAsync(ServiceTicket ticket, CancellationToken ct = default);
    Task<ServiceTicket?> GetTicketAsync(int ticketId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceTicket>> GetOpenTicketsAsync(CancellationToken ct = default);
    Task UpdateStatusAsync(int ticketId, string status, string? notes = null, CancellationToken ct = default);
    Task EscalateAsync(int ticketId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceTicket>> GetCustomerTicketsAsync(int customerId, CancellationToken ct = default);
}

/// <summary>CRM template management (#819-823).</summary>
public interface ICrmTemplateService
{
    Task<IReadOnlyList<CrmTemplate>> GetTemplatesAsync(string? channel = null, CancellationToken ct = default);
    Task<CrmTemplate> SaveTemplateAsync(CrmTemplate template, CancellationToken ct = default);
    Task DeleteTemplateAsync(int templateId, CancellationToken ct = default);
    string RenderTemplate(string content, Dictionary<string, string> mergeFields);
}
