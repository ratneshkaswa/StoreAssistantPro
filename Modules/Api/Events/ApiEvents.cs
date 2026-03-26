using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models.Api;

namespace StoreAssistantPro.Modules.Api.Events;

/// <summary>Published when an API request is received.</summary>
public sealed class ApiRequestReceivedEvent(string clientId, string endpoint) : IEvent
{
    public string ClientId { get; } = clientId;
    public string Endpoint { get; } = endpoint;
}

/// <summary>Published when an API rate limit is exceeded.</summary>
public sealed class ApiRateLimitExceededEvent(string clientId) : IEvent
{
    public string ClientId { get; } = clientId;
}

/// <summary>Published when accounting data is exported.</summary>
public sealed class AccountingExportCompletedEvent(string format, int recordCount) : IEvent
{
    public string Format { get; } = format;
    public int RecordCount { get; } = recordCount;
}

/// <summary>Published when a communication message is sent.</summary>
public sealed class CommunicationSentEvent(CommunicationResult result) : IEvent
{
    public CommunicationResult Result { get; } = result;
}
