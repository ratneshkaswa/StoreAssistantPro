using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class Sale
{
    public int Id { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }

    [Required, MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<SaleItem> Items { get; set; } = [];
}
