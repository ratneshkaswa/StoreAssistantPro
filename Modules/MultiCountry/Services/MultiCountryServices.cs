using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Models.MultiCountry;

namespace StoreAssistantPro.Modules.MultiCountry.Services;

public sealed class CurrencyService(ILogger<CurrencyService> logger) : ICurrencyService
{
    private static readonly List<CurrencyDefinition> Currencies =
    [
        new() { Code = "INR", Symbol = "₹", Name = "Indian Rupee", DecimalPlaces = 2, ExchangeRateToBase = 1m },
        new() { Code = "USD", Symbol = "$", Name = "US Dollar", DecimalPlaces = 2, ExchangeRateToBase = 83.5m },
        new() { Code = "EUR", Symbol = "€", Name = "Euro", DecimalPlaces = 2, ExchangeRateToBase = 91.0m },
        new() { Code = "GBP", Symbol = "£", Name = "British Pound", DecimalPlaces = 2, ExchangeRateToBase = 106.0m }
    ];

    public IReadOnlyList<CurrencyDefinition> GetSupportedCurrencies() => Currencies;
    public CurrencyDefinition GetBaseCurrency() => Currencies[0];

    public decimal Convert(decimal amount, string fromCurrency, string toCurrency)
    {
        var from = Currencies.FirstOrDefault(c => c.Code == fromCurrency);
        var to = Currencies.FirstOrDefault(c => c.Code == toCurrency);
        if (from is null || to is null) return amount;
        return amount * from.ExchangeRateToBase / to.ExchangeRateToBase;
    }

    public Task UpdateExchangeRateAsync(string currencyCode, decimal rate, CancellationToken ct = default)
    {
        var currency = Currencies.FirstOrDefault(c => c.Code == currencyCode);
        if (currency is not null) currency.ExchangeRateToBase = rate;
        logger.LogInformation("Exchange rate updated: {Code} = {Rate}", currencyCode, rate);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ExchangeRate>> GetExchangeRatesAsync(CancellationToken ct = default)
    {
        IReadOnlyList<ExchangeRate> rates = Currencies
            .Where(c => c.Code != "INR")
            .Select(c => new ExchangeRate(c.Code, "INR", c.ExchangeRateToBase, DateTime.UtcNow))
            .ToList();
        return Task.FromResult(rates);
    }

    public string FormatCurrency(decimal amount, string currencyCode)
    {
        var currency = Currencies.FirstOrDefault(c => c.Code == currencyCode) ?? Currencies[0];
        return $"{currency.Symbol}{amount:N2}";
    }
}

public sealed class CountryProfileService(ILogger<CountryProfileService> logger) : ICountryProfileService
{
    private static readonly List<CountryProfile> Profiles =
    [
        new() { CountryCode = "IN", CountryName = "India" },
        new() { CountryCode = "US", CountryName = "United States", CurrencyCode = "USD", TaxSystemName = "Sales Tax", DateFormat = "MM-dd-yyyy", PhoneFormat = "+1 XXX-XXX-XXXX", TaxRegistrationLabel = "EIN", TaxRegistrationFormat = "^\\d{2}-\\d{7}$" },
        new() { CountryCode = "GB", CountryName = "United Kingdom", CurrencyCode = "GBP", TaxSystemName = "VAT", DateFormat = "dd/MM/yyyy", PhoneFormat = "+44 XXXX XXXXXX", TaxRegistrationLabel = "VAT Number", TaxRegistrationFormat = "^GB\\d{9}$" }
    ];

    private string _activeCountry = "IN";

    public IReadOnlyList<CountryProfile> GetSupportedCountries() => Profiles;
    public CountryProfile GetActiveProfile() => Profiles.FirstOrDefault(p => p.CountryCode == _activeCountry) ?? Profiles[0];

    public Task SetActiveProfileAsync(string countryCode, CancellationToken ct = default)
    {
        _activeCountry = countryCode;
        logger.LogInformation("Active country set to {Country}", countryCode);
        return Task.CompletedTask;
    }

    public bool ValidateTaxRegistration(string countryCode, string registrationNumber)
    {
        var profile = Profiles.FirstOrDefault(p => p.CountryCode == countryCode);
        if (profile is null) return false;
        return Regex.IsMatch(registrationNumber, profile.TaxRegistrationFormat);
    }

    public string FormatAddress(string countryCode, Dictionary<string, string> addressParts)
    {
        var profile = Profiles.FirstOrDefault(p => p.CountryCode == countryCode) ?? Profiles[0];
        var result = profile.AddressFormat;
        foreach (var (key, value) in addressParts)
            result = result.Replace($"{{{key}}}", value);
        return result;
    }

    public string FormatPhone(string countryCode, string phoneNumber) => phoneNumber;
}
