using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Template for auto-creating monthly recurring expenses like rent (#234).
/// </summary>
public class RecurringExpense
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>Fixed amount to auto-create each period.</summary>
    public decimal Amount { get; set; }

    /// <summary>Recurrence: Monthly, Weekly, Quarterly, Annually.</summary>
    [MaxLength(20)]
    public string Frequency { get; set; } = "Monthly";

    /// <summary>Day of month the expense is due (1–28).</summary>
    public int DueDay { get; set; } = 1;

    /// <summary>Last date an expense was auto-generated from this template.</summary>
    public DateTime? LastGeneratedDate { get; set; }

    /// <summary>When this recurring expense becomes active.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Optional end date (null = no end).</summary>
    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; }
}
