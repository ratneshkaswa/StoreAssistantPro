using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Tasks.Services;

public class TaskService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional,
    IPerformanceMonitor perf) : ITaskService
{
    public async Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaskService.GetAllAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.TaskItems
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<TaskItem?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaskService.GetByIdAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.TaskItems
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task CreateAsync(TaskDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateDto(dto);

        using var _ = perf.BeginScope("TaskService.CreateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = new TaskItem
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            AssignedTo = dto.AssignedTo?.Trim() ?? string.Empty,
            DueDate = dto.DueDate,
            Priority = dto.Priority,
            Status = "Pending",
            CreatedAt = regional.Now
        };

        context.TaskItems.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(int id, TaskDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateDto(dto);

        using var _ = perf.BeginScope("TaskService.UpdateAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.TaskItems.FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Task with Id {id} not found.");

        entity.Title = dto.Title.Trim();
        entity.Description = dto.Description?.Trim() ?? string.Empty;
        entity.AssignedTo = dto.AssignedTo?.Trim() ?? string.Empty;
        entity.DueDate = dto.DueDate;
        entity.Priority = dto.Priority;
        entity.ModifiedDate = regional.Now;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaskService.DeleteAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.TaskItems.FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Task with Id {id} not found.");

        context.TaskItems.Remove(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SetStatusAsync(int id, string status, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        using var _ = perf.BeginScope("TaskService.SetStatusAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entity = await context.TaskItems.FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Task with Id {id} not found.");

        entity.Status = status;
        entity.ModifiedDate = regional.Now;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<TaskStats> GetStatsAsync(CancellationToken ct = default)
    {
        using var _ = perf.BeginScope("TaskService.GetStatsAsync");
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var pending = await context.TaskItems.CountAsync(t => t.Status == "Pending", ct).ConfigureAwait(false);
        var inProgress = await context.TaskItems.CountAsync(t => t.Status == "In Progress", ct).ConfigureAwait(false);
        var completed = await context.TaskItems.CountAsync(t => t.Status == "Completed", ct).ConfigureAwait(false);
        var total = await context.TaskItems.CountAsync(ct).ConfigureAwait(false);

        return new TaskStats(pending, inProgress, completed, total);
    }

    private static void ValidateDto(TaskDto dto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Title, nameof(dto.Title));
    }
}
