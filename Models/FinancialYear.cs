using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Financial year record for period-based reporting.
/// Indian FY runs April 1 to March 31 (e.g., 2025–26).
/// </summary>
public class FinancialYear
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Name { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>Whether this is the currently active financial year.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Date when the billing counter (invoice number sequence) was last reset.
    /// Typically reset at FY start or manually by admin.
    /// </summary>
    public DateTime? BillingCounterResetDate { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
