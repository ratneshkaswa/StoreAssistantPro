using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Printing;

public partial class PrintPreviewWindow : BaseDialogWindow
{
    private readonly FixedDocument _document;
    private readonly string _zoomStateKey;
    private bool _isUpdatingZoom;

    protected override double DialogWidth => 1200;
    protected override double DialogHeight => 800;
    protected override bool AllowResize => true;
    protected override bool EnableOverflowScrollHost => false;

    public PrintPreviewWindow(
        IWindowSizingService sizingService,
        FixedDocument document,
        string title)
        : base(sizingService)
    {
        _document = document;
        _zoomStateKey = title;
        InitializeComponent();

        TitleText.Text = title;
        DocViewer.Document = document;
        DocViewer.Zoom = PrintPreviewZoomState.Get(_zoomStateKey);
        DocViewer.PageViewsChanged += (_, _) => UpdateViewerState();
        Loaded += (_, _) => Dispatcher.BeginInvoke(UpdateViewerState, DispatcherPriority.ContextIdle);

        WindowState = WindowState.Maximized;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnPreviousPageClick(object sender, RoutedEventArgs e)
    {
        if (!DocViewer.CanGoToPreviousPage)
            return;

        DocViewer.PreviousPage();
        UpdateViewerState();
    }

    private void OnNextPageClick(object sender, RoutedEventArgs e)
    {
        if (!DocViewer.CanGoToNextPage)
            return;

        DocViewer.NextPage();
        UpdateViewerState();
    }

    private void OnZoomSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingZoom || !IsLoaded)
            return;

        var targetZoom = Math.Round(e.NewValue / 10d) * 10d;
        DocViewer.Zoom = targetZoom;
        UpdateViewerState();
    }

    private void OnFitWidthClick(object sender, RoutedEventArgs e)
    {
        DocViewer.FitToMaxPagesAcross(1);
        UpdateViewerState();
    }

    private void OnPrintClick(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
            return;

        dialog.PrintDocument(_document.DocumentPaginator, TitleText.Text);
    }

    private void UpdateViewerState()
    {
        var pageCount = Math.Max(1, DocViewer.PageCount);
        var currentPage = Math.Min(pageCount, Math.Max(1, DocViewer.MasterPageNumber));
        var zoom = Math.Max(80, Math.Min(200, Math.Round(DocViewer.Zoom)));
        PrintPreviewZoomState.Set(_zoomStateKey, zoom);

        PageStatusText.Text = $"Page {currentPage} of {pageCount}";
        PreviousPageButton.IsEnabled = DocViewer.CanGoToPreviousPage;
        NextPageButton.IsEnabled = DocViewer.CanGoToNextPage;
        ZoomStatusText.Text = $"{zoom:0}%";

        _isUpdatingZoom = true;
        ZoomSlider.Value = zoom;
        _isUpdatingZoom = false;
    }
}
