using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Manages UI density mode at runtime. Swaps the density resource
/// dictionary in <c>Application.Resources.MergedDictionaries</c> and
/// publishes <c>DensityChangedEvent</c> so the shell can refresh the
/// current view.
/// <para>
/// <b>Architecture:</b> Registered as a singleton. The density dictionary
/// sits at index 0 in the merged collection, ahead of DesignSystem.xaml,
/// so its token overrides win the last-writer-wins merge.
/// </para>
/// <para>
/// Because the codebase uses <c>StaticResource</c> exclusively, views
/// must be re-instantiated after a density swap. The shell listens to
/// <c>DensityChangedEvent</c> and re-navigates to the current page.
/// </para>
/// </summary>
public interface IDensityService
{
    /// <summary>Current density mode.</summary>
    DensityMode CurrentDensity { get; }

    /// <summary>
    /// Swaps the density resource dictionary and publishes
    /// <c>DensityChangedEvent</c>. The shell re-navigates to
    /// the current page to pick up the new token values.
    /// </summary>
    void ApplyDensity(DensityMode mode);
}
