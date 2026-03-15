using System;
using System.Windows;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Modules.MainShell.Views;

public partial class MainWindow : Window
{
    private const double NotificationPanelEdgeMargin = 16;
    private const double NotificationPanelVerticalMargin = 24;
    private MainViewModel? _boundViewModel;

    public MainWindow(IWindowSizingService sizingService)
    {
        InitializeComponent();
        WindowIconHelper.Apply(this);
        sizingService.ConfigureMainWindow(this);

        SourceInitialized += (_, _) => Win11Backdrop.Apply(this);
        Loaded += (_, _) => UpdateNotificationsPopupLayout();
        SizeChanged += (_, _) => UpdateNotificationsPopupLayout();
        LocationChanged += (_, _) => UpdateNotificationsPopupLayout();
        NotificationBellButton.SizeChanged += (_, _) => UpdateNotificationsPopupLayout();

        DataContextChanged += OnDataContextChanged;
        Closed += OnClosed;
    }

    private void OnNotificationsPopupOpened(object sender, EventArgs e) =>
        UpdateNotificationsPopupLayout();

    internal static double CalculateNotificationPanelWidth(
        double preferredWidth,
        double availableWidth,
        double anchorWidth)
    {
        var maxWidth = Math.Max(anchorWidth, availableWidth);
        return Math.Max(anchorWidth, Math.Min(preferredWidth, maxWidth));
    }

    internal static double CalculateNotificationPopupOffset(double panelWidth, double anchorWidth) =>
        anchorWidth - panelWidth;

    internal static double CalculateNotificationPanelMaxHeight(double preferredMaxHeight, double availableHeight)
    {
        if (availableHeight <= 0)
            return preferredMaxHeight;

        return Math.Min(preferredMaxHeight, availableHeight);
    }

    private void UpdateNotificationsPopupLayout()
    {
        if (!IsLoaded
            || NotificationBellButton.ActualWidth <= 0
            || NotificationBellButton.ActualHeight <= 0
            || ActualHeight <= 0)
        {
            return;
        }

        var bellOrigin = NotificationBellButton.TranslatePoint(new Point(0, 0), this);
        var bellRight = bellOrigin.X + NotificationBellButton.ActualWidth;
        var bellBottom = bellOrigin.Y + NotificationBellButton.ActualHeight;

        var preferredWidth = GetResourceDouble("NotificationPanelWidth", 320);
        var preferredMaxHeight = GetResourceDouble("NotificationPanelMaxHeight", 400);
        var availableWidth = bellRight - NotificationPanelEdgeMargin;
        var availableHeight = ActualHeight - bellBottom - NotificationPanelVerticalMargin;

        var panelWidth = CalculateNotificationPanelWidth(
            preferredWidth,
            availableWidth,
            NotificationBellButton.ActualWidth);

        NotificationsPanel.Width = panelWidth;
        NotificationsPanel.MaxHeight = CalculateNotificationPanelMaxHeight(preferredMaxHeight, availableHeight);
        NotificationsPopup.HorizontalOffset = CalculateNotificationPopupOffset(
            panelWidth,
            NotificationBellButton.ActualWidth);
    }

    private double GetResourceDouble(string key, double fallback) =>
        TryFindResource(key) is double value ? value : fallback;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
            oldVm.RequestClose = null;

        if (e.NewValue is MainViewModel newVm)
        {
            _boundViewModel = newVm;
            newVm.RequestClose = Close;
            newVm.ApplyShortcuts(this);
        }
        else
        {
            _boundViewModel = null;
            InputBindings.Clear();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_boundViewModel is not null)
        {
            _boundViewModel.RequestClose = null;
            _boundViewModel = null;
        }

        (DataContext as IDisposable)?.Dispose();
    }
}
