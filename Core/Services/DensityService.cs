using System.Windows;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Swaps the density resource dictionary at runtime so spacing and
/// control height tokens change globally. Because the codebase uses
/// <c>StaticResource</c> exclusively, the density dictionary must be
/// merged <b>after</b> DesignSystem.xaml (last writer wins) and views
/// must be re-instantiated to pick up the new values.
/// <para>
/// <b>Dictionary placement:</b> The density dictionary is always the
/// <b>last</b> entry in <c>Application.Resources.MergedDictionaries</c>
/// so its tokens override every earlier dictionary.
/// </para>
/// <para>
/// <b>Refresh strategy:</b> After the swap, the service publishes
/// <see cref="DensityChangedEvent"/>. The shell subscribes and
/// re-navigates to the current page, which re-creates the
/// <c>UserControl</c> DataTemplate with fresh <c>StaticResource</c>
/// lookups against the new dictionary values.
/// </para>
/// </summary>
public sealed class DensityService : IDensityService
{
    private readonly IEventBus _eventBus;
    private ResourceDictionary? _currentDensityDict;

    private static readonly Uri NormalUri = new("Core/Styles/DensityNormal.xaml", UriKind.Relative);
    private static readonly Uri CompactUri = new("Core/Styles/DensityCompact.xaml", UriKind.Relative);

    public DensityMode CurrentDensity { get; private set; } = DensityMode.Normal;

    public DensityService(IEventBus eventBus)
    {
        _eventBus = eventBus;

        // Insert the default (Normal) density dictionary at the end
        // of the merged collection so it can be swapped later.
        _currentDensityDict = LoadDictionary(NormalUri);
        Application.Current.Resources.MergedDictionaries.Add(_currentDensityDict);
    }

    public void ApplyDensity(DensityMode mode)
    {
        if (mode == CurrentDensity)
            return;

        var merged = Application.Current.Resources.MergedDictionaries;

        // Remove the old density dictionary
        if (_currentDensityDict is not null)
            merged.Remove(_currentDensityDict);

        // Load and append the new one (last position = highest priority)
        var uri = mode switch
        {
            DensityMode.Compact => CompactUri,
            _ => NormalUri
        };

        _currentDensityDict = LoadDictionary(uri);
        merged.Add(_currentDensityDict);

        CurrentDensity = mode;

        // Publish so the shell can re-navigate / refresh the current page
        _ = _eventBus.PublishAsync(new DensityChangedEvent(mode));
    }

    private static ResourceDictionary LoadDictionary(Uri relativeUri)
    {
        return new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/{relativeUri}", UriKind.Absolute)
        };
    }
}
