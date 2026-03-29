using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace StoreAssistantPro.Core.Printing;

/// <summary>
/// Converts text-based reports to WPF <see cref="FixedDocument"/> for printing and preview (#447-#449, #454).
/// </summary>
public static class ReportPrintHelper
{
    private const double DefaultMargin = 40;
    private static readonly Typeface MonoTypeface = new("Consolas");
    private const double FontSize = 13;
    private const double LineHeight = 17;

    /// <summary>Creates a <see cref="FixedDocument"/> from plain text report content.</summary>
    public static FixedDocument CreateDocument(string reportText, double pageWidth = 816, double pageHeight = 1056)
    {
        var doc = new FixedDocument();
        doc.DocumentPaginator.PageSize = new Size(pageWidth, pageHeight);

        var lines = reportText.Split('\n');
        var usableHeight = pageHeight - DefaultMargin * 2;
        var linesPerPage = (int)(usableHeight / LineHeight);
        var pageIndex = 0;

        while (pageIndex < lines.Length)
        {
            var page = new FixedPage { Width = pageWidth, Height = pageHeight };

            var panel = new StackPanel { Margin = new Thickness(DefaultMargin) };

            var count = Math.Min(linesPerPage, lines.Length - pageIndex);
            for (var i = 0; i < count; i++, pageIndex++)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = lines[pageIndex].TrimEnd('\r'),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = FontSize,
                    Foreground = Brushes.Black
                });
            }

            page.Children.Add(panel);

            var content = new PageContent();
            ((System.Windows.Markup.IAddChild)content).AddChild(page);
            doc.Pages.Add(content);
        }

        return doc;
    }

    /// <summary>Prints a text report directly via <see cref="PrintDialog"/>.</summary>
    public static bool PrintReport(string reportText, string documentTitle)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return false;

        var doc = CreateDocument(reportText, dialog.PrintableAreaWidth, dialog.PrintableAreaHeight);
        dialog.PrintDocument(doc.DocumentPaginator, documentTitle);
        return true;
    }
}
