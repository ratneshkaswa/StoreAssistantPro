namespace StoreAssistantPro.Models;

/// <summary>
/// Categories to which a tax slab is applicable in the clothing domain.
/// </summary>
public enum TaxApplicableCategory
{
    /// <summary>Applicable only to readymade garments.</summary>
    Readymade,

    /// <summary>Applicable only to garment cloth/fabric.</summary>
    GarmentCloth,

    /// <summary>Applicable to both readymade and garment cloth.</summary>
    Both
}
