namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Read-only provider of application-level metadata.
/// Singleton — injected into ViewModels and services that
/// need version, environment, or database status information.
/// </summary>
public interface IApplicationInfoService
{
    string AppVersion { get; }
    string DotNetVersion { get; }
    string Environment { get; }
    string DatabaseServer { get; }
    string DatabaseName { get; }
    string LogDirectory { get; }

    Task<bool> IsDatabaseConnectedAsync();
    Task<IReadOnlyList<string>> GetPendingMigrationsAsync();
}
