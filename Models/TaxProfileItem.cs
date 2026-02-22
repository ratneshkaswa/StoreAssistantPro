namespace StoreAssistantPro.Models;

/// <summary>
/// Join entity linking a <see cref="TaxProfile"/> to a <see cref="TaxMaster"/> component.
/// Each item represents one tax line within the composite profile.
/// </summary>
public class TaxProfileItem
{
    public int Id { get; set; }

    public int TaxProfileId { get; set; }
    public TaxProfile? TaxProfile { get; set; }

    public int TaxMasterId { get; set; }
    public TaxMaster? TaxMaster { get; set; }
}
