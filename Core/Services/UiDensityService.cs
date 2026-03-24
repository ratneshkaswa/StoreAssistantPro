using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Applies the app-wide normal/compact density tokens used by shared list and grid styles.
/// </summary>
public partial class UiDensityService : ObservableObject, IUiDensityService
{
    private const string NormalRowHeightKey = "NormalDensityDataGridRowHeight";
    private const string CompactRowHeightKey = "CompactDensityDataGridRowHeight";
    private const string ActiveRowHeightKey = "AppDataGridRowHeight";

    private const string NormalCellPaddingKey = "NormalDensityDataGridCellPadding";
    private const string CompactCellPaddingKey = "CompactDensityDataGridCellPadding";
    private const string ActiveCellPaddingKey = "AppDataGridCellPadding";

    private const string NormalHeaderPaddingKey = "NormalDensityDataGridHeaderPadding";
    private const string CompactHeaderPaddingKey = "CompactDensityDataGridHeaderPadding";
    private const string ActiveHeaderPaddingKey = "AppDataGridHeaderPadding";

    [ObservableProperty]
    public partial bool IsCompactModeEnabled { get; set; }

    public UiDensityService()
    {
        ApplyCurrentDensity();
    }

    public void SetCompactMode(bool enabled)
    {
        if (IsCompactModeEnabled == enabled)
            return;

        IsCompactModeEnabled = enabled;
    }

    partial void OnIsCompactModeEnabledChanged(bool value) => ApplyCurrentDensity();

    private void ApplyCurrentDensity()
    {
        var application = Application.Current;
        if (application is null)
            return;

        application.Resources[ActiveRowHeightKey] = ResolveDouble(
            application,
            IsCompactModeEnabled ? CompactRowHeightKey : NormalRowHeightKey,
            IsCompactModeEnabled ? 28d : 32d);

        application.Resources[ActiveCellPaddingKey] = ResolveThickness(
            application,
            IsCompactModeEnabled ? CompactCellPaddingKey : NormalCellPaddingKey,
            IsCompactModeEnabled ? new Thickness(12, 4, 12, 4) : new Thickness(12, 8, 12, 8));

        application.Resources[ActiveHeaderPaddingKey] = ResolveThickness(
            application,
            IsCompactModeEnabled ? CompactHeaderPaddingKey : NormalHeaderPaddingKey,
            IsCompactModeEnabled ? new Thickness(12, 4, 12, 4) : new Thickness(12, 8, 12, 8));
    }

    private static double ResolveDouble(Application application, string key, double fallback) =>
        application.TryFindResource(key) is double value ? value : fallback;

    private static Thickness ResolveThickness(Application application, string key, Thickness fallback) =>
        application.TryFindResource(key) is Thickness value ? value : fallback;
}
