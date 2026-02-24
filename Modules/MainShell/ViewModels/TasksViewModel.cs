using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Modules.MainShell.Services;

namespace StoreAssistantPro.Modules.MainShell.ViewModels;

/// <summary>
/// ViewModel for the Tasks dialog. Generates pending tasks
/// from current dashboard data (low stock, zero sales, etc.)
/// and allows dismissing them.
/// </summary>
public partial class TasksViewModel(IDashboardService dashboardService) : BaseViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial ObservableCollection<string> Tasks { get; set; } = [];

    public bool IsEmpty => Tasks.Count == 0;

    [RelayCommand]
    private async Task LoadTasksAsync()
    {
        try
        {
            var summary = await dashboardService.GetSummaryAsync();
            var tasks = new List<string>();

            if (summary.LowStockCount > 0)
                tasks.Add($"⚠ {summary.LowStockCount} product(s) are low on stock — review inventory.");

            foreach (var product in summary.LowStockProducts.Take(5))
            {
                tasks.Add($"📦 '{product.Name}' has only {product.Quantity} left in stock.");
            }

            if (summary.TodaysTransactions == 0)
                tasks.Add("💰 No sales recorded today — check if the system is operational.");

            if (summary.TotalProducts == 0)
                tasks.Add("📋 No products in the system — add products to get started.");

            Tasks = new ObservableCollection<string>(tasks);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void DismissTask(string? task)
    {
        if (task is not null)
            Tasks.Remove(task);
        OnPropertyChanged(nameof(IsEmpty));
    }
}
