using CommunityToolkit.Mvvm.ComponentModel;

namespace StoreAssistantPro.Core.Features;

/// <summary>
/// In-memory feature flag store. Defaults to enabled for all
/// unknown features (opt-out model: disable explicitly).
/// <para>
/// Call <see cref="Load"/> once at startup with the flags read
/// from configuration. After loading, raises
/// <see cref="ObservableObject.PropertyChanged"/> so any bound
/// ViewModel properties refresh automatically.
/// </para>
/// </summary>
public partial class FeatureToggleService : ObservableObject, IFeatureToggleService
{
    private Dictionary<string, bool> _flags = [];

    public IReadOnlyDictionary<string, bool> AllFlags => _flags;

    public bool IsEnabled(string featureName) =>
        !_flags.TryGetValue(featureName, out var enabled) || enabled;

    public void Load(IReadOnlyDictionary<string, bool> flags)
    {
        _flags = new Dictionary<string, bool>(flags, StringComparer.OrdinalIgnoreCase);

        // Notify all bound properties so UI refreshes
        OnPropertyChanged(string.Empty);
    }
}
