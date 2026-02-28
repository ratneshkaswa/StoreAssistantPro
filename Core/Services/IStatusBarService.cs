using System.ComponentModel;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Centralized service for status bar messages. ViewModels and services
/// post messages here; the UI binds to <see cref="Message"/>.
/// <para>
/// Messages posted with a duration auto-clear after the specified time,
/// reverting to <see cref="DefaultMessage"/>.  Messages posted without
/// a duration persist until replaced.
/// </para>
/// </summary>
public interface IStatusBarService : INotifyPropertyChanged
{
    /// <summary>Current status bar text (bind the UI to this).</summary>
    string Message { get; }

    /// <summary>
    /// Fallback text shown when no transient message is active.
    /// Defaults to <c>"Ready"</c>.
    /// </summary>
    string DefaultMessage { get; set; }

    /// <summary>
    /// Post a status message that auto-clears after <paramref name="duration"/>.
    /// </summary>
    void Post(string message, TimeSpan duration);

    /// <summary>
    /// Post a status message that auto-clears after 4 seconds.
    /// </summary>
    void Post(string message);

    /// <summary>
    /// Set a persistent message that stays until replaced or cleared.
    /// </summary>
    void SetPersistent(string message);

    /// <summary>
    /// Clear the current message and revert to <see cref="DefaultMessage"/>.
    /// </summary>
    void Clear();
}
