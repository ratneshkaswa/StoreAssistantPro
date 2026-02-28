using System.Collections.ObjectModel;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Shows transient toast notifications in the bottom-right corner of
/// the main window. Toasts auto-dismiss after a configurable duration.
/// <para>
/// <b>Architecture:</b> Registered as a singleton. ViewModels and services
/// call <see cref="Show"/> to display a toast. The <c>ToastHost</c>
/// control in MainWindow binds to <see cref="Toasts"/> to render them.
/// </para>
/// <para>
/// <b>Integration:</b> The service also subscribes to
/// <c>NotificationPostedEvent</c> so every notification posted through
/// <see cref="INotificationService"/> automatically spawns a toast.
/// </para>
/// </summary>
public interface IToastService
{
    /// <summary>
    /// Live collection of active toasts. The <c>ToastHost</c> control
    /// binds to this collection.
    /// </summary>
    ObservableCollection<ToastItem> Toasts { get; }

    /// <summary>
    /// Display a toast with the given message and severity level.
    /// The toast auto-removes after <paramref name="duration"/>
    /// (defaults to 4 seconds).
    /// </summary>
    void Show(string message,
              AppNotificationLevel level = AppNotificationLevel.Info,
              TimeSpan? duration = null);

    /// <summary>
    /// Immediately remove a specific toast (e.g. on manual dismiss).
    /// </summary>
    void Dismiss(Guid toastId);
}
