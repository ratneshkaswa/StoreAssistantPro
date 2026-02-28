namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Tracks which inline guidance tips have been dismissed by the user.
/// State is persisted to a local settings file so dismissed tips stay
/// hidden across application restarts and window navigations.
/// <para>
/// <b>Key format:</b> Each tip is identified by a unique string key
/// scoped to a window or view, e.g. <c>"SalesView.QuickTipScanner"</c>.
/// Keys are case-insensitive.
/// </para>
/// <para>
/// <b>Usage in a ViewModel:</b>
/// </para>
/// <code>
/// // Check on load
/// IsScannerTipDismissed = tipState.IsTipDismissed("SalesView.QuickTipScanner");
///
/// // Persist on dismiss (called by InlineTipBanner.DismissCommand)
/// tipState.DismissTip("SalesView.QuickTipScanner");
/// </code>
/// <para>
/// <b>Lifetime:</b> Registered as a singleton. Thread-safe for
/// concurrent reads from multiple ViewModels.
/// </para>
/// </summary>
public interface ITipStateService
{
    /// <summary>
    /// Returns <c>true</c> if the tip identified by <paramref name="tipKey"/>
    /// has been dismissed by the user. The check is performed against an
    /// in-memory cache that is populated from the settings file on first access.
    /// </summary>
    /// <param name="tipKey">
    /// Unique tip identifier, e.g. <c>"SalesView.QuickTipScanner"</c>.
    /// Case-insensitive.
    /// </param>
    bool IsTipDismissed(string tipKey);

    /// <summary>
    /// Records that the tip identified by <paramref name="tipKey"/> has been
    /// dismissed. The state is written to the in-memory cache immediately and
    /// flushed to the settings file asynchronously.
    /// </summary>
    /// <param name="tipKey">
    /// Unique tip identifier, e.g. <c>"SalesView.QuickTipScanner"</c>.
    /// Case-insensitive.
    /// </param>
    void DismissTip(string tipKey);

    /// <summary>
    /// Resets a previously dismissed tip so it becomes visible again.
    /// Useful for "Reset all tips" functionality in system settings.
    /// </summary>
    /// <param name="tipKey">
    /// Unique tip identifier to restore. Case-insensitive.
    /// </param>
    void ResetTip(string tipKey);

    /// <summary>
    /// Resets all dismissed tips so every tip becomes visible again.
    /// Called from system settings when the user wants to re-see all
    /// guidance banners.
    /// </summary>
    void ResetAll();
}
