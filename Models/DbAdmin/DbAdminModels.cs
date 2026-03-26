namespace StoreAssistantPro.Models.DbAdmin;

/// <summary>
/// Database size and health status (#867).
/// </summary>
public sealed record DatabaseStatus(
    long SizeBytes,
    string FormattedSize,
    int TableCount,
    long TotalRows,
    string SchemaVersion,
    DateTime LastOptimized,
    bool NeedsOptimization);

/// <summary>
/// Migration info for the migration manager (#869).
/// </summary>
public sealed record MigrationInfo(
    string MigrationId,
    string Name,
    bool IsApplied,
    DateTime? AppliedAt);

/// <summary>
/// Query performance log entry (#874).
/// </summary>
public sealed record SlowQueryEntry(
    string QueryText,
    double ElapsedMs,
    DateTime ExecutedAt,
    string? Source);

/// <summary>
/// Data purge result (#871).
/// </summary>
public sealed record PurgeResult(
    int RecordsPurged,
    int TablesAffected,
    long SpaceReclaimed,
    DateTime PurgedAt,
    string? BackupPath);

/// <summary>
/// DB export/import result (#875, #876).
/// </summary>
public sealed record DataTransferResult(
    bool Success,
    int EntitiesProcessed,
    int RecordsProcessed,
    string? FilePath,
    string? Error);

/// <summary>
/// Connection pool status (#878).
/// </summary>
public sealed record ConnectionPoolStatus(
    int ActiveConnections,
    int IdleConnections,
    int MaxPoolSize,
    int PendingRequests,
    double PoolUtilizationPercent);

/// <summary>
/// Automatic maintenance schedule configuration (#880).
/// </summary>
public sealed class MaintenanceSchedule
{
    public bool IsEnabled { get; set; } = true;
    public TimeSpan RunAtTime { get; set; } = new(2, 0, 0); // 2 AM
    public bool VacuumEnabled { get; set; } = true;
    public bool IndexRebuildEnabled { get; set; } = true;
    public bool PurgeOldLogsEnabled { get; set; }
    public int PurgeLogsOlderThanDays { get; set; } = 90;
    public DateTime? LastRunAt { get; set; }
}
