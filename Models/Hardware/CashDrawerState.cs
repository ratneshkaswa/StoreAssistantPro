namespace StoreAssistantPro.Models.Hardware;

/// <summary>Cash drawer operational state.</summary>
public sealed class CashDrawerState
{
    public bool IsOpen { get; set; }
    public int OpenCountThisShift { get; set; }
    public DateTime? LastOpenedUtc { get; set; }
    public string? AssignedRegister { get; set; }
}
