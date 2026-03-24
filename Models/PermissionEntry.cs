using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Stores per-feature permission grants for non-admin users (#289).
/// Admin always has full access — only User-type grants are stored.
/// </summary>
public class PermissionEntry
{
    public int Id { get; set; }

    /// <summary>Feature key matching FeatureFlags or module names.</summary>
    [Required, MaxLength(100)]
    public string FeatureKey { get; set; } = string.Empty;

    /// <summary>Whether the User role can access this feature.</summary>
    public bool IsAllowed { get; set; }
}
