using StoreAssistantPro.Models.DbAdmin;

namespace StoreAssistantPro.Modules.DbAdmin.Services;

/// <summary>
/// Database monitoring and health service (#867, #877, #878).
/// </summary>
public interface IDatabaseMonitorService
{
    /// <summary>Get current database size and status (#867).</summary>
    Task<DatabaseStatus> GetDatabaseStatusAsync(CancellationToken ct = default);

    /// <summary>Get current schema version (#877).</summary>
    Task<string> GetSchemaVersionAsync(CancellationToken ct = default);

    /// <summary>Get connection pool status (#878).</summary>
    Task<ConnectionPoolStatus> GetConnectionPoolStatusAsync(CancellationToken ct = default);

    /// <summary>Get recent slow queries (#874).</summary>
    Task<IReadOnlyList<SlowQueryEntry>> GetSlowQueriesAsync(int maxResults = 50, CancellationToken ct = default);

    /// <summary>Detect deadlocks in the database (#879).</summary>
    Task<IReadOnlyList<string>> DetectDeadlocksAsync(CancellationToken ct = default);
}

/// <summary>
/// Database optimization and maintenance service (#868, #873, #880).
/// </summary>
public interface IDatabaseMaintenanceService
{
    /// <summary>Optimize/vacuum the database to reclaim space (#868).</summary>
    Task<long> OptimizeDatabaseAsync(CancellationToken ct = default);

    /// <summary>Rebuild database indexes (#873).</summary>
    Task RebuildIndexesAsync(CancellationToken ct = default);

    /// <summary>Run automatic maintenance based on schedule (#880).</summary>
    Task RunAutomaticMaintenanceAsync(MaintenanceSchedule schedule, CancellationToken ct = default);

    /// <summary>Get or set the maintenance schedule (#880).</summary>
    MaintenanceSchedule GetMaintenanceSchedule();
    void SetMaintenanceSchedule(MaintenanceSchedule schedule);
}

/// <summary>
/// Migration management service (#869).
/// </summary>
public interface IMigrationManagerService
{
    /// <summary>Get all migrations and their applied status (#869).</summary>
    Task<IReadOnlyList<MigrationInfo>> GetMigrationsAsync(CancellationToken ct = default);

    /// <summary>Apply pending migrations (#869).</summary>
    Task<int> ApplyPendingMigrationsAsync(CancellationToken ct = default);
}

/// <summary>
/// Seed data and purge service (#870, #871, #872).
/// </summary>
public interface IDataManagementService
{
    /// <summary>Load sample/demo data (#870).</summary>
    Task<int> LoadSeedDataAsync(CancellationToken ct = default);

    /// <summary>Purge records older than N years (#871).</summary>
    Task<PurgeResult> PurgeOldRecordsAsync(int olderThanYears, bool createBackup = true, CancellationToken ct = default);

    /// <summary>Archive old sales to archive table (#872).</summary>
    Task<int> ArchiveOldSalesAsync(int olderThanMonths = 24, CancellationToken ct = default);
}

/// <summary>
/// Database export and import service (#875, #876).
/// </summary>
public interface IDataTransferService
{
    /// <summary>Export entire database as JSON (#875).</summary>
    Task<DataTransferResult> ExportToJsonAsync(string outputDirectory, CancellationToken ct = default);

    /// <summary>Import database from JSON files (#876).</summary>
    Task<DataTransferResult> ImportFromJsonAsync(string inputDirectory, CancellationToken ct = default);
}
