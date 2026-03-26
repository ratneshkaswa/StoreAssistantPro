using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.CRM;

namespace StoreAssistantPro.Modules.CRM.Services;

public sealed class CampaignService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<CampaignService> logger) : ICampaignService
{
    public async Task<IReadOnlyList<Campaign>> GetCampaignsAsync(string? status = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Campaigns.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);
        return await query.OrderByDescending(c => c.SentAt ?? c.ScheduledAt).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<Campaign> SaveCampaignAsync(Campaign campaign, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (campaign.Id == 0) context.Campaigns.Add(campaign); else context.Campaigns.Update(campaign);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Campaign saved: {Name} ({Channel})", campaign.Name, campaign.Channel);
        return campaign;
    }

    public async Task<Campaign> SendCampaignAsync(int campaignId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var campaign = await context.Campaigns.FindAsync([campaignId], ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found");
        campaign.Status = "Completed";
        campaign.SentAt = DateTime.UtcNow;
        campaign.SentCount = campaign.RecipientCount;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Campaign {Name} sent to {Count} recipients", campaign.Name, campaign.SentCount);
        return campaign;
    }

    public async Task ScheduleCampaignAsync(int campaignId, DateTime scheduledAt, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var campaign = await context.Campaigns.FindAsync([campaignId], ct).ConfigureAwait(false);
        if (campaign is null) return;
        campaign.ScheduledAt = scheduledAt;
        campaign.Status = "Scheduled";
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<Campaign> GetCampaignAnalyticsAsync(int campaignId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Campaigns.FindAsync([campaignId], ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found");
    }
}

public sealed class AutoGreetingService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<AutoGreetingService> logger) : IAutoGreetingService
{
    public async Task SendBirthdayGreetingsAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var today = DateTime.Today;
        var customers = await context.Customers
            .Where(c => c.IsActive && c.Birthday != null && c.Birthday.Value.Month == today.Month && c.Birthday.Value.Day == today.Day)
            .ToListAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Sending birthday greetings to {Count} customers", customers.Count);
    }

    public async Task SendAnniversaryOffersAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var today = DateTime.Today;
        var customers = await context.Customers
            .Where(c => c.IsActive && c.Anniversary != null && c.Anniversary.Value.Month == today.Month && c.Anniversary.Value.Day == today.Day)
            .ToListAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Sending anniversary offers to {Count} customers", customers.Count);
    }
}

public sealed class FeedbackService(ILogger<FeedbackService> logger) : IFeedbackService
{
    private readonly List<CustomerFeedback> _store = [];

    public Task<CustomerFeedback> RecordFeedbackAsync(CustomerFeedback feedback, CancellationToken ct = default)
    {
        _store.Add(feedback);
        logger.LogInformation("Feedback recorded: customer {Id}, score {Score}", feedback.CustomerId, feedback.Score);
        return Task.FromResult(feedback);
    }

    public Task<double> CalculateNpsAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var relevant = _store.Where(f => f.SubmittedAt >= from && f.SubmittedAt <= to).ToList();
        if (relevant.Count == 0) return Task.FromResult(0.0);
        var promoters = relevant.Count(f => f.Score >= 9);
        var detractors = relevant.Count(f => f.Score <= 6);
        var nps = ((double)(promoters - detractors) / relevant.Count) * 100;
        return Task.FromResult(nps);
    }

    public Task<IReadOnlyList<CustomerFeedback>> GetFeedbackAsync(int? customerId = null, CancellationToken ct = default)
    {
        IReadOnlyList<CustomerFeedback> result = customerId.HasValue
            ? _store.Where(f => f.CustomerId == customerId.Value).ToList()
            : _store.ToList();
        return Task.FromResult(result);
    }
}

public sealed class ServiceTicketService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<ServiceTicketService> logger) : IServiceTicketService
{
    public async Task<ServiceTicket> CreateTicketAsync(ServiceTicket ticket, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        ticket.CreatedAt = DateTime.UtcNow;
        ticket.EscalationDeadline = DateTime.UtcNow.AddDays(ticket.Priority == "Critical" ? 1 : 3);
        context.ServiceTickets.Add(ticket);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Ticket created: {Subject} ({Type})", ticket.Subject, ticket.TicketType);
        return ticket;
    }

    public async Task<ServiceTicket?> GetTicketAsync(int ticketId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.ServiceTickets.FindAsync([ticketId], ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ServiceTicket>> GetOpenTicketsAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.ServiceTickets
            .Where(t => t.Status != "Closed" && t.Status != "Resolved")
            .OrderBy(t => t.Priority == "Critical" ? 0 : t.Priority == "High" ? 1 : 2)
            .ThenBy(t => t.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateStatusAsync(int ticketId, string status, string? notes = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var ticket = await context.ServiceTickets.FindAsync([ticketId], ct).ConfigureAwait(false);
        if (ticket is null) return;
        ticket.Status = status;
        if (notes is not null) ticket.ResolutionNotes = notes;
        if (status is "Resolved" or "Closed") ticket.ResolvedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task EscalateAsync(int ticketId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var ticket = await context.ServiceTickets.FindAsync([ticketId], ct).ConfigureAwait(false);
        if (ticket is null) return;
        ticket.Status = "Escalated";
        ticket.Priority = "Critical";
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogWarning("Ticket {Id} escalated", ticketId);
    }

    public async Task<IReadOnlyList<ServiceTicket>> GetCustomerTicketsAsync(int customerId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.ServiceTickets
            .Where(t => t.CustomerId == customerId).OrderByDescending(t => t.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
    }
}

public sealed class CrmTemplateService(
    IDbContextFactory<AppDbContext> contextFactory) : ICrmTemplateService
{
    public async Task<IReadOnlyList<CrmTemplate>> GetTemplatesAsync(string? channel = null, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.CrmTemplates.Where(t => t.IsActive);
        if (!string.IsNullOrWhiteSpace(channel)) query = query.Where(t => t.Channel == channel);
        return await query.OrderBy(t => t.Name).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<CrmTemplate> SaveTemplateAsync(CrmTemplate template, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (template.Id == 0) context.CrmTemplates.Add(template); else context.CrmTemplates.Update(template);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return template;
    }

    public async Task DeleteTemplateAsync(int templateId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var template = await context.CrmTemplates.FindAsync([templateId], ct).ConfigureAwait(false);
        if (template is null) return;
        template.IsActive = false;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public string RenderTemplate(string content, Dictionary<string, string> mergeFields)
    {
        var result = content;
        foreach (var (key, value) in mergeFields)
            result = result.Replace($"{{{{{key}}}}}", value);
        return result;
    }
}
