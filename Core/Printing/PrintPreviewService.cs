using System.Windows;
using System.Windows.Documents;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Printing;

/// <summary>
/// Shows a print preview dialog with a <see cref="DocumentViewer"/> (#454).
/// </summary>
public sealed class PrintPreviewService(IWindowSizingService sizingService) : IPrintPreviewService
{
    public void ShowPreview(string reportText, string title)
    {
        var doc = ReportPrintHelper.CreateDocument(reportText);
        ShowPreview(doc, title);
    }

    public void ShowPreview(FixedDocument document, string title)
    {
        var window = new PrintPreviewWindow(sizingService, document, title);
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
    }
}
