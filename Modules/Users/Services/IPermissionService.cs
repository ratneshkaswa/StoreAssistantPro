using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Users.Services;

/// <summary>
/// Manages per-feature permission grants for non-admin users (#289).
/// Admin always has full access — permissions only restrict User role.
/// </summary>
public interface IPermissionService
{
    /// <summary>Checks whether the given user type can access a feature.</summary>
    Task<bool> HasPermissionAsync(UserType userType, string featureKey, CancellationToken ct = default);

    /// <summary>Returns all permission entries (for the permission matrix UI).</summary>
    Task<IReadOnlyList<PermissionEntry>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Saves a batch of permission entries (full replace).</summary>
    Task SaveAsync(IReadOnlyList<PermissionEntry> entries, CancellationToken ct = default);

    /// <summary>Returns all defined feature keys for the permission matrix.</summary>
    IReadOnlyList<string> GetAllFeatureKeys();
}
