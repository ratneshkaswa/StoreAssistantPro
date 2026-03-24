using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class UserCredential
{
    public int Id { get; set; }

    public UserType UserType { get; set; }

    [Required]
    public string PinHash { get; set; } = string.Empty;

    public int FailedAttempts { get; set; }

    public DateTime? LockoutEndTime { get; set; }

    /// <summary>User display name (#281).</summary>
    [MaxLength(100)]
    public string? DisplayName { get; set; }

    /// <summary>User email (#281).</summary>
    [MaxLength(100)]
    public string? Email { get; set; }

    /// <summary>User phone (#281).</summary>
    [MaxLength(15)]
    public string? Phone { get; set; }
}
