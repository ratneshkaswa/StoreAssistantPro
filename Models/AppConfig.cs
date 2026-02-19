using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class AppConfig
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string FirmName { get; set; } = string.Empty;

    public bool IsInitialized { get; set; }

    [Required]
    public string MasterPinHash { get; set; } = string.Empty;
}
