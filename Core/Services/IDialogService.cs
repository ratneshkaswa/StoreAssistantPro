namespace StoreAssistantPro.Core.Services;

/// <summary>
/// ViewModel-facing dialog API. Uses string-based dialog keys so
/// ViewModels never reference another module's View types.
/// </summary>
public interface IDialogService
{
    bool Confirm(string message, string title = "Confirm");
    bool? ShowDialog(string dialogKey);
}
