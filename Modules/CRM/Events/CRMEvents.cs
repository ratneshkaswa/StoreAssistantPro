using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models.CRM;

namespace StoreAssistantPro.Modules.CRM.Events;

/// <summary>Published when a campaign is sent.</summary>
public sealed class CampaignSentEvent(Campaign campaign) : IEvent
{
    public Campaign Campaign { get; } = campaign;
}

/// <summary>Published when a service ticket is resolved.</summary>
public sealed class TicketResolvedEvent(int ticketId) : IEvent
{
    public int TicketId { get; } = ticketId;
}

/// <summary>Published when customer feedback is submitted.</summary>
public sealed class FeedbackReceivedEvent(int customerId, int score) : IEvent
{
    public int CustomerId { get; } = customerId;
    public int Score { get; } = score;
}
