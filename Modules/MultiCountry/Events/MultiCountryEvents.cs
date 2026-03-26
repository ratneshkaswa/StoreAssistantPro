using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Modules.MultiCountry.Events;

/// <summary>Published when the active country profile changes.</summary>
public sealed class CountryProfileChangedEvent(string countryCode) : IEvent
{
    public string CountryCode { get; } = countryCode;
}

/// <summary>Published when exchange rates are refreshed.</summary>
public sealed class ExchangeRatesRefreshedEvent(int rateCount) : IEvent
{
    public int RateCount { get; } = rateCount;
}
