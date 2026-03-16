using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Products.Services;

/// <summary>
/// Holds the selected product context when navigating from
/// Product Management to Variant Management. Registered as Singleton.
/// </summary>
public class ProductContextHolder
{
    public Product? SelectedProduct { get; set; }
}
