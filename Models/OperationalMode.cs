namespace StoreAssistantPro.Models;

/// <summary>
/// Controls which feature set is active across the application.
/// <para>
/// <b>Management</b> — Full access to product catalog, tax configuration,
/// user management, reports, and settings. Billing is available but
/// secondary.<br/>
/// <b>Billing</b> — Streamlined POS mode focused on cart, payment, and
/// receipt. Management features are hidden or read-only to keep the
/// UI distraction-free for counter operators.
/// </para>
/// <para>
/// The current mode is stored in <see cref="Core.Services.IAppStateService"/>
/// and drives navigation visibility, toolbar composition, and keyboard
/// shortcut sets throughout the shell.
/// </para>
/// </summary>
public enum OperationalMode
{
    /// <summary>
    /// Back-office mode: catalog, tax, users, reports, settings.
    /// </summary>
    Management = 0,

    /// <summary>
    /// Point-of-sale mode: cart, payment, receipts.
    /// </summary>
    Billing = 1
}
