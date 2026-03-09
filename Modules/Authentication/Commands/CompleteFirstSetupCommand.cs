using StoreAssistantPro.Core.Commands;

namespace StoreAssistantPro.Modules.Authentication.Commands;

/// <summary>
/// Business/system options collected during one-time setup that go into
/// <c>AppConfig</c> and <c>SystemSettings</c>.
/// </summary>
public sealed record SetupBusinessOptions(
    string GstRegistrationType,
    decimal CompositionSchemeRate,
    string? StateCode,
    string DefaultTaxMode,
    string RoundingMethod,
    string NumberToWordsLanguage,
    bool NegativeStockAllowed,
    bool AutoBackupEnabled,
    string? BackupTime,
    string? BackupLocation);

public sealed record CompleteFirstSetupCommand(
    string FirmName,
    string Address,
    string State,
    string Pincode,
    string Phone,
    string Email,
    string GSTIN,
    string PAN,
    string CurrencyCode,
    string CurrencySymbol,
    int FinancialYearStartMonth,
    int FinancialYearEndMonth,
    string DateFormat,
    string AdminPin,
    string ManagerPin,
    string UserPin,
    string MasterPin,
    SetupBusinessOptions BusinessOptions) : ICommandRequest<Unit>;
