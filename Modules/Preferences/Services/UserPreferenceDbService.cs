using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.Preferences;

namespace StoreAssistantPro.Modules.Preferences.Services;

public sealed class UserPreferenceDbService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regionalSettings,
    ILogger<UserPreferenceDbService> logger) : IUserPreferenceDbService
{
    public async Task<string?> GetPreferenceAsync(int userId, string key, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var pref = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Key == key, ct).ConfigureAwait(false);
        return pref?.Value;
    }

    public async Task SetPreferenceAsync(int userId, string key, string? value, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var pref = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Key == key, ct).ConfigureAwait(false);

        if (pref is null)
        {
            pref = new UserPreference { UserId = userId, Key = key, Value = value, UpdatedAt = DateTime.UtcNow };
            context.UserPreferences.Add(pref);
        }
        else
        {
            pref.Value = value;
            pref.UpdatedAt = DateTime.UtcNow;
        }
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<string, string?>> GetAllPreferencesAsync(int userId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.UserPreferences
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.Key, p => p.Value, ct).ConfigureAwait(false);
    }

    public async Task DeletePreferenceAsync(int userId, string key, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await context.UserPreferences
            .Where(p => p.UserId == userId && p.Key == key)
            .ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }

    public async Task<string?> GetDefaultLandingPageAsync(int userId, CancellationToken ct = default)
        => await GetPreferenceAsync(userId, PreferenceKeys.DefaultLandingPage, ct).ConfigureAwait(false);

    public async Task<string?> GetDefaultViewModeAsync(int userId, CancellationToken ct = default)
        => await GetPreferenceAsync(userId, PreferenceKeys.DefaultViewMode, ct).ConfigureAwait(false);

    public async Task<int> GetItemsPerPageAsync(int userId, int fallback = 20, CancellationToken ct = default)
    {
        var val = await GetPreferenceAsync(userId, PreferenceKeys.ItemsPerPage, ct).ConfigureAwait(false);
        return int.TryParse(val, out var result) ? result : fallback;
    }

    public async Task<string?> GetDefaultPrinterAsync(int userId, CancellationToken ct = default)
        => await GetPreferenceAsync(userId, PreferenceKeys.DefaultPrinter, ct).ConfigureAwait(false);

    public async Task<string?> GetDefaultPaymentMethodAsync(int userId, CancellationToken ct = default)
        => await GetPreferenceAsync(userId, PreferenceKeys.DefaultPaymentMethod, ct).ConfigureAwait(false);

    public async Task<bool> GetSidebarExpandedAsync(int userId, CancellationToken ct = default)
    {
        var val = await GetPreferenceAsync(userId, PreferenceKeys.SidebarState, ct).ConfigureAwait(false);
        return val != "Collapsed";
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetColumnVisibilityAsync(int userId, string viewName, CancellationToken ct = default)
    {
        var key = $"{PreferenceKeys.ColumnVisibility}:{viewName}";
        var json = await GetPreferenceAsync(userId, key, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, bool>();
        return JsonSerializer.Deserialize<Dictionary<string, bool>>(json) ?? new Dictionary<string, bool>();
    }

    public async Task SetColumnVisibilityAsync(int userId, string viewName, IReadOnlyDictionary<string, bool> columns, CancellationToken ct = default)
    {
        var key = $"{PreferenceKeys.ColumnVisibility}:{viewName}";
        var json = JsonSerializer.Serialize(columns);
        await SetPreferenceAsync(userId, key, json, ct).ConfigureAwait(false);
    }

    public async Task<(string Column, bool Ascending)?> GetSortPreferenceAsync(int userId, string viewName, CancellationToken ct = default)
    {
        var key = $"{PreferenceKeys.SortPreference}:{viewName}";
        var json = await GetPreferenceAsync(userId, key, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json)) return null;
        var parts = json.Split('|');
        return parts.Length == 2 ? (parts[0], parts[1] == "Asc") : null;
    }

    public async Task SetSortPreferenceAsync(int userId, string viewName, string column, bool ascending, CancellationToken ct = default)
    {
        var key = $"{PreferenceKeys.SortPreference}:{viewName}";
        await SetPreferenceAsync(userId, key, $"{column}|{(ascending ? "Asc" : "Desc")}", ct).ConfigureAwait(false);
    }

    public async Task<string?> GetFilterPreferenceAsync(int userId, string viewName, CancellationToken ct = default)
    {
        var key = $"{PreferenceKeys.FilterPreference}:{viewName}";
        return await GetPreferenceAsync(userId, key, ct).ConfigureAwait(false);
    }

    public async Task SetFilterPreferenceAsync(int userId, string viewName, string filterJson, CancellationToken ct = default)
    {
        var key = $"{PreferenceKeys.FilterPreference}:{viewName}";
        await SetPreferenceAsync(userId, key, filterJson, ct).ConfigureAwait(false);
    }

    public async Task<bool> IsNotificationEnabledAsync(int userId, string channel, CancellationToken ct = default)
    {
        var val = await GetPreferenceAsync(userId, channel, ct).ConfigureAwait(false);
        return val != "false";
    }

    public async Task SetNotificationEnabledAsync(int userId, string channel, bool enabled, CancellationToken ct = default)
        => await SetPreferenceAsync(userId, channel, enabled ? "true" : "false", ct).ConfigureAwait(false);

    public async Task<string?> GetAlertSoundAsync(int userId, CancellationToken ct = default)
        => await GetPreferenceAsync(userId, PreferenceKeys.AlertSound, ct).ConfigureAwait(false);

    public async Task<int?> GetLowStockThresholdAsync(int userId, CancellationToken ct = default)
    {
        var val = await GetPreferenceAsync(userId, PreferenceKeys.LowStockThreshold, ct).ConfigureAwait(false);
        return int.TryParse(val, out var result) ? result : null;
    }

    public async Task<QuietHoursConfig?> GetQuietHoursAsync(int userId, CancellationToken ct = default)
    {
        var startVal = await GetPreferenceAsync(userId, PreferenceKeys.QuietHoursStart, ct).ConfigureAwait(false);
        var endVal = await GetPreferenceAsync(userId, PreferenceKeys.QuietHoursEnd, ct).ConfigureAwait(false);
        if (startVal is null || endVal is null) return null;

        return TimeSpan.TryParse(startVal, out var start) && TimeSpan.TryParse(endVal, out var end)
            ? new QuietHoursConfig(start, end, true)
            : null;
    }

    public async Task SetQuietHoursAsync(int userId, QuietHoursConfig config, CancellationToken ct = default)
    {
        await SetPreferenceAsync(userId, PreferenceKeys.QuietHoursStart, config.Start.ToString(), ct).ConfigureAwait(false);
        await SetPreferenceAsync(userId, PreferenceKeys.QuietHoursEnd, config.End.ToString(), ct).ConfigureAwait(false);
    }

    public async Task<bool> IsInQuietHoursAsync(int userId, CancellationToken ct = default)
    {
        var config = await GetQuietHoursAsync(userId, ct).ConfigureAwait(false);
        if (config is null || !config.IsEnabled) return false;

        var now = regionalSettings.Now.TimeOfDay;
        return config.Start <= config.End
            ? now >= config.Start && now <= config.End
            : now >= config.Start || now <= config.End;
    }

    public async Task<string?> GetNotificationPriorityFilterAsync(int userId, CancellationToken ct = default)
        => await GetPreferenceAsync(userId, PreferenceKeys.NotificationPriority, ct).ConfigureAwait(false);

    public async Task RecordDismissedNotificationAsync(int userId, NotificationHistoryEntry entry, CancellationToken ct = default)
    {
        var history = await GetNotificationHistoryAsync(userId, 100, ct).ConfigureAwait(false);
        var list = history.ToList();
        list.Insert(0, entry);
        if (list.Count > 100) list.RemoveRange(100, list.Count - 100);

        var json = JsonSerializer.Serialize(list);
        await SetPreferenceAsync(userId, PreferenceKeys.NotificationHistory, json, ct).ConfigureAwait(false);
        logger.LogDebug("Recorded notification dismissal for user {UserId}: {Title}", userId, entry.Title);
    }

    public async Task<IReadOnlyList<NotificationHistoryEntry>> GetNotificationHistoryAsync(int userId, int maxResults = 50, CancellationToken ct = default)
    {
        var json = await GetPreferenceAsync(userId, PreferenceKeys.NotificationHistory, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json)) return [];
        var list = JsonSerializer.Deserialize<List<NotificationHistoryEntry>>(json) ?? [];
        return list.Take(maxResults).ToList();
    }
}
