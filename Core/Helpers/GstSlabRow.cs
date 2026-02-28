namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Data row for the GST rate slab reference table in TaxWindow.
/// Used as a static DataGrid item in XAML.
/// </summary>
public class GstSlabRow
{
    public string Slab { get; set; } = string.Empty;
    public string CGST { get; set; } = string.Empty;
    public string SGST { get; set; } = string.Empty;
    public string IGST { get; set; } = string.Empty;
    public string Examples { get; set; } = string.Empty;
}
