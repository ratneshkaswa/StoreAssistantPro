namespace StoreAssistantPro.Models;

/// <summary>
/// Classifies a product for tax applicability and attribute selection.
/// </summary>
public enum ProductType
{
    /// <summary>Finished garments sold by piece (Shirt, Pant, Kurta).</summary>
    Readymade,

    /// <summary>Fabric sold by the meter (Cotton, Silk, Polyester).</summary>
    GarmentCloth
}
