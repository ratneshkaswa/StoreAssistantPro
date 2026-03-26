namespace StoreAssistantPro.Models.MultiCountry;

/// <summary>Supported currency (#718, #725).</summary>
public sealed class CurrencyDefinition
{
    public string Code { get; set; } = "INR";
    public string Symbol { get; set; } = "₹";
    public string Name { get; set; } = "Indian Rupee";
    public int DecimalPlaces { get; set; } = 2;
    public decimal ExchangeRateToBase { get; set; } = 1m;
}

/// <summary>Country profile for multi-country support (#718-726).</summary>
public sealed class CountryProfile
{
    public string CountryCode { get; set; } = "IN";
    public string CountryName { get; set; } = "India";
    public string CurrencyCode { get; set; } = "INR";
    public string TaxSystemName { get; set; } = "GST";
    public string DateFormat { get; set; } = "dd-MM-yyyy";
    public string PhoneFormat { get; set; } = "+91 XXXXX XXXXX";
    public string TaxRegistrationLabel { get; set; } = "GSTIN";
    public string TaxRegistrationFormat { get; set; } = "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$";
    public string AddressFormat { get; set; } = "{Line1}\n{Line2}\n{City}, {State} {Pincode}";
}

/// <summary>Currency exchange rate entry (#725).</summary>
public sealed record ExchangeRate(
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateTime EffectiveDate);
