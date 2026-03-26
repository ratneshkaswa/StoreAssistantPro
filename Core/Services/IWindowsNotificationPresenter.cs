using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Presents app notifications through the Windows Notification Center.
/// </summary>
public interface IWindowsNotificationPresenter
{
    void EnsureRegistered();

    bool TryShow(AppNotification notification);
}
