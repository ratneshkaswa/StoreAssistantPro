using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreAssistantPro.Models;

public class TaskItem
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(100)]
    public string AssignedTo { get; set; } = string.Empty;

    public DateTime? DueDate { get; set; }

    [MaxLength(50)]
    public string Priority { get; set; } = "Medium";

    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    public DateTime? ModifiedDate { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    [NotMapped]
    public bool IsOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.Today && Status != "Completed";

    [NotMapped]
    public string DueDateDisplay => DueDate.HasValue
        ? (IsOverdue ? $"Overdue \u00b7 {DueDate.Value:dd-MMM}" : $"Due: {DueDate.Value:dd-MMM}")
        : "";
}
