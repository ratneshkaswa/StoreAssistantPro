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
}
