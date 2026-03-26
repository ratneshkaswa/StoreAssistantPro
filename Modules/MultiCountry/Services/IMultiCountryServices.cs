using StoreAssistantPro.Models.MultiCountry;

namespace StoreAssistantPro.Modules.MultiCountry.Services;

/// <summary>Multi-currency service (#718, #725, #933).</summary>
public interface ICurrencyService
{
    IReadOnlyList<CurrencyDefinition> GetSupportedCurrencies();
    CurrencyDefinition GetBaseCurrency();
    decimal Convert(decimal amount, string fromCurrency, string toCurrency);
    Task UpdateExchangeRateAsync(string currencyCode, decimal rate, CancellationToken ct = default);
    Task<IReadOnlyList<ExchangeRate>> GetExchangeRatesAsync(CancellationToken ct = default);
    string FormatCurrency(decimal amount, string currencyCode);
}

/// <summary>Country profile service (#719-726).</summary>
public interface ICountryProfileService
{
    IReadOnlyList<CountryProfile> GetSupportedCountries();
    CountryProfile GetActiveProfile();
    Task SetActiveProfileAsync(string countryCode, CancellationToken ct = default);
    bool ValidateTaxRegistration(string countryCode, string registrationNumber);
    string FormatAddress(string countryCode, Dictionary<string, string> addressParts);
    string FormatPhone(string countryCode, string phoneNumber);
}
