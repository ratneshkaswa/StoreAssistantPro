using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class Payment
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public DateTime PaymentDate { get; set; }

    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string Note { get; set; } = string.Empty;

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
