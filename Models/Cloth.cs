using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class Cloth
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public DateTime CreatedDate { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
