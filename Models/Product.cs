using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
