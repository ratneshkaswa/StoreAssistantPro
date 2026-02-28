using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

public class Category
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public RowHighlightLevel HighlightLevel =>
        !IsActive ? RowHighlightLevel.Inactive : RowHighlightLevel.None;
}
