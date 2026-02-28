namespace StoreAssistantPro.Modules.SystemSettings.Services;

/// <summary>
/// Result of a database integrity check.
/// </summary>
public sealed record DatabaseIntegrityResult(
    bool IsHealthy,
    string Details,
    long DatabaseSizeBytes);
