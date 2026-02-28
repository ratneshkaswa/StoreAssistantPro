using System.ComponentModel;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Features;

/// <summary>
/// Singleton service that answers "is feature X enabled?" for the
/// entire application. Loaded once at startup from configuration.
/// <para>
/// Implements <see cref="INotifyPropertyChanged"/> so XAML can
/// bind visibility directly to feature state when needed through
/// the ViewModel layer.
/// </para>
/// <para>
/// <b>Mode filtering:</b> After <see cref="SetMode"/> is called,
/// <see cref="IsEnabled"/> returns <c>false</c> for features that
/// are not available in the current <see cref="OperationalMode"/>,
/// regardless of the config flag value.
/// </para>
/// </summary>
public interface IFeatureToggleService : INotifyPropertyChanged
{
    bool IsEnabled(string featureName);
    OperationalMode CurrentMode { get; }
    IReadOnlyDictionary<string, bool> AllFlags { get; }
    void Load(IReadOnlyDictionary<string, bool> flags);
    void SetMode(OperationalMode mode);
}
