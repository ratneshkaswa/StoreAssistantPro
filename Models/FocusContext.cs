namespace StoreAssistantPro.Models;

/// <summary>
/// Snapshot of the application state at the moment a focus decision
/// is needed. Passed to <see cref="Core.Services.IFocusRuleEngine"/>
/// so rules can select the correct landing target.
/// <para>
/// <b>Examples:</b>
/// </para>
/// <code>
/// // Page navigation in management mode
/// new FocusContext(OperationalMode.Management, "Vendors", FocusContextType.Page)
///
/// // Billing lock acquired
/// new FocusContext(OperationalMode.Billing, "Billing", FocusContextType.Page)
///
/// // Dialog opened
/// new FocusContext(OperationalMode.Management, "FirmManagement", FocusContextType.Dialog)
///
/// // Bottom form revealed
/// new FocusContext(OperationalMode.Management, "Vendors", FocusContextType.Form)
/// </code>
/// </summary>
/// <param name="Mode">The active operational mode.</param>
/// <param name="ContextKey">
/// The page key, dialog key, or form name that identifies the
/// <see cref="FocusMap"/> to consult.
/// </param>
/// <param name="ContextType">The type of UI container.</param>
public sealed record FocusContext(
    OperationalMode Mode,
    string ContextKey,
    FocusContextType ContextType);
