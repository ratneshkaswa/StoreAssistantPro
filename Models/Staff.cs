using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Staff/employee record for billing attribution, incentive tracking,
/// and sales performance analysis.
/// </summary>
public class Staff
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? StaffCode { get; set; }

    [MaxLength(15)]
    public string? Phone { get; set; }

    [MaxLength(50)]
    public string? Role { get; set; }

    /// <summary>Normal incentive rate (percentage on non-incentivable sales).</summary>
    public decimal NormalIncentiveRate { get; set; }

    /// <summary>Special incentive rate (percentage on incentivable sales).</summary>
    public decimal SpecialIncentiveRate { get; set; }

    /// <summary>Daily sales target amount.</summary>
    public decimal DailyTarget { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime JoinDate { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
