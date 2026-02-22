using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Named tax profile that groups one or more <see cref="TaxMaster"/> components
/// into a composite tax applied to products.
/// <para>
/// Examples for Indian GST:
/// <list type="bullet">
///   <item>"GST 5%" → CGST 2.5% + SGST 2.5%</item>
///   <item>"GST 18%" → CGST 9% + SGST 9%</item>
///   <item>"IGST 18%" → IGST 18% (single component)</item>
///   <item>"GST 28% + Cess" → CGST 14% + SGST 14% + Cess 12%</item>
///   <item>"Exempt" → no components, effective rate 0%</item>
/// </list>
/// </para>
/// </summary>
public class TaxProfile
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string ProfileName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsDefault { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Tax components that make up this profile.
    /// The effective tax rate is the sum of all component rates.
    /// </summary>
    public ICollection<TaxProfileItem> Items { get; set; } = [];
}
