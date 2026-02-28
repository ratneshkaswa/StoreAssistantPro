namespace StoreAssistantPro.Models;

/// <summary>
/// Controls the spacing and control height density across the UI.
/// <list type="bullet">
///   <item><see cref="Normal"/> — Default enterprise spacing (32 px controls, 16 px card padding).</item>
///   <item><see cref="Compact"/> — Reduced spacing for data-dense screens (26 px controls, 10 px card padding).</item>
/// </list>
/// </summary>
public enum DensityMode
{
    Normal,
    Compact
}
