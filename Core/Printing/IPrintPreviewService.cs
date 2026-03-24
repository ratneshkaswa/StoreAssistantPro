using System.Windows.Documents;

namespace StoreAssistantPro.Core.Printing;

/// <summary>Service for showing print preview dialogs (#454).</summary>
public interface IPrintPreviewService
{
    /// <summary>Shows a print preview dialog for the given text report.</summary>
    void ShowPreview(string reportText, string title);

    /// <summary>Shows a print preview dialog for a <see cref="FixedDocument"/>.</summary>
    void ShowPreview(FixedDocument document, string title);
}
