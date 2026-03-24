using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Opt-in attached property for DataGrids that should expose Win11-style
/// multi-select row selectors instead of the hover-only grip affordance.
/// </summary>
public static class DataGridMultiSelect
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridMultiSelect),
            new FrameworkPropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid)
            return;

        if ((bool)e.NewValue)
        {
            dataGrid.SelectionMode = DataGridSelectionMode.Extended;
            dataGrid.SelectionUnit = DataGridSelectionUnit.FullRow;
        }
    }
}
