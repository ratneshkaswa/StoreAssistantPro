using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Modules.Localization.Events;

/// <summary>Published when regional settings (number/date format) change.</summary>
public sealed class RegionalSettingsChangedEvent(string settingName, string newValue) : IEvent
{
    public string SettingName { get; } = settingName;
    public string NewValue { get; } = newValue;
}

/// <summary>Published when state-specific tax labels are refreshed.</summary>
public sealed class TaxLabelsRefreshedEvent(string stateCode) : IEvent
{
    public string StateCode { get; } = stateCode;
}
