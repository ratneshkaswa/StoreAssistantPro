namespace StoreAssistantPro.Models;

public class PriceHistory
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal OldSalePrice { get; set; }
    public decimal NewSalePrice { get; set; }
    public decimal OldCostPrice { get; set; }
    public decimal NewCostPrice { get; set; }
    public decimal OldMRP { get; set; }
    public decimal NewMRP { get; set; }
    public DateTime ChangedDate { get; set; }
    public string? ChangedBy { get; set; }
}
