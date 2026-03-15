using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Tasks.Services;

namespace StoreAssistantPro.Modules.Tasks.ViewModels;

public partial class TaskManagementViewModel(ITaskService taskService) : BaseViewModel
{
    private List<TaskItem> _allItems = [];

    // ── Collections ──

    [ObservableProperty]
    public partial ObservableCollection<TaskItem> Tasks { get; set; } = [];

    // ── Stat counters ──

    [ObservableProperty]
    public partial int PendingCount { get; set; }

    [ObservableProperty]
    public partial int InProgressCount { get; set; }

    [ObservableProperty]
    public partial int CompletedCount { get; set; }

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    // ── Form fields ──

    [ObservableProperty]
    public partial string TaskTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AssignedTo { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime? DueDate { get; set; }

    [ObservableProperty]
    public partial int SelectedPriorityIndex { get; set; } = 1;

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial string SaveButtonText { get; set; } = "Save";

    // ── Filters ──

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ActiveDateFilter { get; set; } = "All";

    [ObservableProperty]
    public partial string ActiveStatusFilter { get; set; } = "All";

    [ObservableProperty]
    public partial string FilterCountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasItems { get; set; } = true;

    // ── Selection ──

    [ObservableProperty]
    public partial TaskItem? SelectedTask { get; set; }

    private int? _editingId;

    // ── Load ──

    [RelayCommand]
    private Task LoadAsync() => RunLoadAsync(async ct =>
    {
        var stats = await taskService.GetStatsAsync(ct);
        PendingCount = stats.Pending;
        InProgressCount = stats.InProgress;
        CompletedCount = stats.Completed;
        TotalCount = stats.Total;

        var items = await taskService.GetAllAsync(ct);
        _allItems = [.. items];
        ApplyFilters();
    });

    // ── Search ──

    [RelayCommand]
    private void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            ApplyFilters();
            return;
        }
        var query = SearchText;
        var filtered = _allItems
            .Where(t => (t.Title ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                     || (t.AssignedTo ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                     || (t.Description ?? "").Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
        Tasks = new ObservableCollection<TaskItem>(filtered);
        HasItems = filtered.Count > 0;
        FilterCountText = $"{filtered.Count} results";
    }

    // ── Filters ──

    [RelayCommand]
    private void SetDateFilter(string filter)
    {
        ActiveDateFilter = filter;
        ActiveStatusFilter = "All";
        ApplyFilters();
    }

    [RelayCommand]
    private void SetStatusFilter(string filter)
    {
        ActiveStatusFilter = filter;
        ActiveDateFilter = "All";
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var today = DateTime.Today;
        IEnumerable<TaskItem> filtered = _allItems;

        if (ActiveDateFilter != "All")
        {
            filtered = ActiveDateFilter switch
            {
                "Today" => filtered.Where(t => t.CreatedAt.Date == today),
                "Week" => filtered.Where(t => t.CreatedAt.Date >= today.AddDays(-(int)today.DayOfWeek)),
                "Month" => filtered.Where(t => t.CreatedAt.Year == today.Year && t.CreatedAt.Month == today.Month),
                _ => filtered
            };
        }

        if (ActiveStatusFilter != "All")
        {
            filtered = filtered.Where(t => t.Status == ActiveStatusFilter);
        }

        var list = filtered.ToList();
        Tasks = new ObservableCollection<TaskItem>(list);
        HasItems = list.Count > 0;
        FilterCountText = (ActiveDateFilter == "All" && ActiveStatusFilter == "All") ? "" : $"{list.Count} tasks";
    }

    // ── CRUD ──

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v.Rule(!string.IsNullOrWhiteSpace(TaskTitle), "Task title is required.")))
            return;

        var priority = SelectedPriorityIndex switch
        {
            0 => "Low",
            2 => "High",
            _ => "Medium"
        };

        var dto = new TaskDto(TaskTitle, Description, AssignedTo, DueDate, priority);

        if (IsEditing && _editingId.HasValue)
        {
            await taskService.UpdateAsync(_editingId.Value, dto, ct);
            SuccessMessage = "Task updated.";
        }
        else
        {
            await taskService.CreateAsync(dto, ct);
            SuccessMessage = "Task added.";
        }

        ResetForm();
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private void Edit(TaskItem? task)
    {
        if (task is null) return;

        _editingId = task.Id;
        TaskTitle = task.Title;
        Description = task.Description;
        AssignedTo = task.AssignedTo;
        DueDate = task.DueDate;
        SelectedPriorityIndex = task.Priority switch
        {
            "Low" => 0,
            "High" => 2,
            _ => 1
        };
        IsEditing = true;
        SaveButtonText = "Update";
    }

    [RelayCommand]
    private Task DeleteAsync(TaskItem? task) => RunAsync(async ct =>
    {
        if (task is null) return;

        await taskService.DeleteAsync(task.Id, ct);
        SuccessMessage = "Task deleted.";
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task MarkPendingAsync(TaskItem? task) => SetStatusAsync(task, "Pending");

    [RelayCommand]
    private Task MarkInProgressAsync(TaskItem? task) => SetStatusAsync(task, "In Progress");

    [RelayCommand]
    private Task MarkCompletedAsync(TaskItem? task) => SetStatusAsync(task, "Completed");

    private Task SetStatusAsync(TaskItem? task, string status) => RunAsync(async ct =>
    {
        if (task is null) return;

        await taskService.SetStatusAsync(task.Id, status, ct);
        SuccessMessage = $"Task marked as {status}.";
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private void ClearForm() => ResetForm();

    private void ResetForm()
    {
        _editingId = null;
        TaskTitle = string.Empty;
        Description = string.Empty;
        AssignedTo = string.Empty;
        DueDate = null;
        SelectedPriorityIndex = 1;
        IsEditing = false;
        SaveButtonText = "Save";
    }

    private async Task ReloadAsync(CancellationToken ct)
    {
        var stats = await taskService.GetStatsAsync(ct);
        PendingCount = stats.Pending;
        InProgressCount = stats.InProgress;
        CompletedCount = stats.Completed;
        TotalCount = stats.Total;

        var items = await taskService.GetAllAsync(ct);
        _allItems = [.. items];
        ApplyFilters();
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
