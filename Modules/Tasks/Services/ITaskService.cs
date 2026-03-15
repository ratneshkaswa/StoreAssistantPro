using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Tasks.Services;

public interface ITaskService
{
    Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken ct = default);
    Task<TaskItem?> GetByIdAsync(int id, CancellationToken ct = default);
    Task CreateAsync(TaskDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, TaskDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task SetStatusAsync(int id, string status, CancellationToken ct = default);
    Task<TaskStats> GetStatsAsync(CancellationToken ct = default);
}

public record TaskDto(
    string Title,
    string? Description,
    string? AssignedTo,
    DateTime? DueDate,
    string Priority);

public record TaskStats(
    int Pending,
    int InProgress,
    int Completed,
    int Total);
