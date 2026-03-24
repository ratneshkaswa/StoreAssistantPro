using System.ComponentModel;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Controls the shared runtime density mode used by list and grid surfaces.
/// </summary>
public interface IUiDensityService : INotifyPropertyChanged
{
    bool IsCompactModeEnabled { get; }

    void SetCompactMode(bool enabled);
}
