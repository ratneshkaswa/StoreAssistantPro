namespace StoreAssistantPro.Models;

/// <summary>
/// A single element in a <see cref="FocusMap"/>'s logical focus order.
/// <para>
/// Each entry maps a logical name (matching the <c>x:Name</c> or
/// <c>AutomationProperties.AutomationId</c> in XAML) to a
/// <see cref="FocusRole"/> and optional guard condition.
/// </para>
///
/// <para><b>Example:</b></para>
/// <code>
/// new FocusMapEntry("NameInput", FocusRole.PrimaryInput)
/// new FocusMapEntry("SaveButton", FocusRole.PrimaryAction)
/// new FocusMapEntry("SearchBox", FocusRole.SearchInput, IsAvailableWhen: "IsToolbarVisible")
/// </code>
/// </summary>
/// <param name="ElementName">
/// The <c>x:Name</c> of the target element in the visual tree.
/// Must be unique within the owning <see cref="FocusMap"/>.
/// </param>
/// <param name="Role">
/// The workflow role this element plays. Used by
/// <see cref="Core.Services.IPredictiveFocusService"/> to pick the
/// right landing target for each transition type.
/// </param>
/// <param name="IsAvailableWhen">
/// Optional ViewModel property name that must be <c>true</c> for this
/// entry to be eligible. When <c>null</c>, the entry is always eligible.
/// Used for conditional elements like collapsible form fields.
/// </param>
public sealed record FocusMapEntry(
    string ElementName,
    FocusRole Role,
    string? IsAvailableWhen = null);
