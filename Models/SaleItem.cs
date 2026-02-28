using System.ComponentModel.DataAnnotations.Schema;

namespace StoreAssistantPro.Models;

public class SaleItem
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    [NotMapped]
    public decimal Subtotal => Quantity * UnitPrice;

    public int SaleId { get; set; }
    public Sale? Sale { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }
}
