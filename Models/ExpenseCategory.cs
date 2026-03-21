using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Expense category for grouping expenses (#227).
/// Predefined: Rent, Utilities, Salary, Transport, Packaging, Marketing, Maintenance, Misc.
/// </summary>
public class ExpenseCategory
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>System-seeded categories cannot be deleted.</summary>
    public bool IsSystem { get; set; }

    public bool IsActive { get; set; } = true;

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
