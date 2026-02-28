using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// ViewModel-facing dialog API. Uses string-based dialog keys so
/// ViewModels never reference another module's View types.
/// </summary>
public interface IDialogService
{
    bool Confirm(string message, string title = "Confirm");
    string? PromptPassword(string message, string title = "Authentication Required");
    bool? ShowDialog(string dialogKey);

    /// <summary>
    /// Shows the Resume Billing dialog with session details.
    /// Returns <c>true</c> if the user chose to resume,
    /// <c>false</c> if the user chose to discard.
    /// </summary>
    bool ShowResumeBillingDialog(BillingSession session, UserType currentUserType);
}
