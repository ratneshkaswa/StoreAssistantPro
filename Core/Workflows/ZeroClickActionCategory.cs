namespace StoreAssistantPro.Core.Workflows;

/// <summary>
/// Classifies the type of action a <see cref="IZeroClickRule"/> performs.
/// Used by <see cref="IZeroClickSafetyPolicy"/> to determine whether
/// automatic execution is permitted.
/// </summary>
public enum ZeroClickActionCategory
{
    /// <summary>
    /// Read-only operations — view data, refresh lists, navigate.
    /// Always safe for zero-click execution.
    /// </summary>
    ReadOnly,

    /// <summary>
    /// Navigation — switch pages, open panels, focus fields.
    /// Always safe for zero-click execution.
    /// </summary>
    Navigation,

    /// <summary>
    /// Data entry — add product to cart, auto-fill form fields.
    /// Safe for zero-click when confidence is high.
    /// </summary>
    DataEntry,

    /// <summary>
    /// Delete or remove operations — delete product, remove vendor,
    /// clear cart. <b>Never</b> allowed for zero-click execution.
    /// </summary>
    Delete,

    /// <summary>
    /// Settings or configuration changes — change tax rate, modify
    /// backup schedule, toggle features. <b>Never</b> allowed for
    /// zero-click execution.
    /// </summary>
    SettingsChange,

    /// <summary>
    /// Financial confirmations — complete sale, process refund,
    /// apply discount, adjust pricing. <b>Never</b> allowed for
    /// zero-click execution.
    /// </summary>
    FinancialConfirmation,

    /// <summary>
    /// Security-sensitive operations — change PIN, modify user roles,
    /// export data. <b>Never</b> allowed for zero-click execution.
    /// </summary>
    SecuritySensitive
}
