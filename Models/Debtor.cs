using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreAssistantPro.Models;

public class Debtor
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(15)]
    public string Phone { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    [NotMapped]
    public decimal Balance => TotalAmount - PaidAmount;

    public DateTime Date { get; set; } = DateTime.Today;

    [MaxLength(500)]
    public string Note { get; set; } = string.Empty;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    [NotMapped]
    public string DaysAgo => (DateTime.Today - Date.Date).Days switch
    {
        0 => "Today",
        1 => "1 day",
        var d => $"{d}d"
    };
}
