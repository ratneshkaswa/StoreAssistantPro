using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Indian state master for GST place-of-supply rules.
/// Each state has a unique 2-digit GST state code used in GSTIN
/// and for determining intra-state (CGST+SGST) vs inter-state (IGST) supply.
/// </summary>
public class State
{
    public int Id { get; set; }

    /// <summary>2-digit GST state code (e.g., "27" for Maharashtra).</summary>
    [Required, MaxLength(2)]
    public string StateCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>Whether this state is the business home state (for CGST/SGST vs IGST determination).</summary>
    public bool IsHomeState { get; set; }
}
