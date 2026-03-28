using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Modules.MainShell.Views;

public partial class MainWindow : Window
{
    private const int WmNcLButtonDown = 0x00A1;
    private const int WmNcLButtonDblClk = 0x00A3;
    private const int WmSysCommand = 0x0112;
    private const int ScMove = 0xF010;
    private static readonly IntPtr HtCaption = new(2);
    private const double NotificationPanelEdgeMargin = 16;
    private const double NotificationPanelVerticalMargin = 24;
    private MainViewModel? _boundViewModel;

    private Core.Controls.ResponsiveContentControl ShellContentHost =>
        (Core.Controls.ResponsiveContentControl)ShellContentPanel.Children
            .OfType<Core.Controls.ResponsiveContentControl>().First();

    public MainWindow(IWindowSizingService sizingService)
    {
        InitializeComponent();
        WindowIconHelper.Apply(this);
        sizingService.ConfigureMainWindow(this);

        SourceInitialized += OnSourceInitialized;
        Loaded += (_, _) => UpdateNotificationsPopupLayout();
        Loaded += (_, _) => InitializeQuickActionBarChrome();
        SizeChanged += (_, _) => UpdateNotificationsPopupLayout();
        LocationChanged += (_, _) => UpdateNotificationsPopupLayout();
        NotificationBellButton.SizeChanged += (_, _) => UpdateNotificationsPopupLayout();
        ShellContentHost.ScrollOffsetChanged += OnShellContentScrollChanged;

        DataContextChanged += OnDataContextChanged;
        Closed += OnClosed;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        Win11Backdrop.Apply(this);

        if (PresentationSource.FromVisual(this) is HwndSource source)
            source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmNcLButtonDown || msg == WmNcLButtonDblClk)
        {
            if (wParam == HtCaption)
            {
                handled = true;
                return IntPtr.Zero;
            }
        }

        if (msg == WmSysCommand && ((wParam.ToInt64() & 0xFFF0) == ScMove))
        {
            handled = true;
            return IntPtr.Zero;
        }

        return IntPtr.Zero;
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

    private void InitializeQuickActionBarChrome()
    {
        if (QuickActionBarHost.Visibility != Visibility.Visible)
            return;

        QuickActionBarHost.MaxHeight = double.PositiveInfinity;
        QuickActionBarHost.Opacity = 1;
        QuickActionBarTransform.Y = 0;
        QuickActionBarShadow.Opacity = 0;
    }

    private void OnShellContentScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        UpdateQuickActionBarShadow(e.VerticalOffset > 0.5);
    }

    private void UpdateQuickActionBarShadow(bool showShadow)
    {
        var targetOpacity = showShadow ? 1d : 0d;
        if (Math.Abs(QuickActionBarShadow.Opacity - targetOpacity) < 0.01)
            return;

        QuickActionBarShadow.BeginAnimation(OpacityProperty, null);
        QuickActionBarShadow.Opacity = targetOpacity;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
        {
            oldVm.RequestClose = null;
            oldVm.PropertyChanged -= OnMainViewModelPropertyChanged;
        }

        if (e.NewValue is MainViewModel newVm)
        {
            _boundViewModel = newVm;
            newVm.RequestClose = Close;
            newVm.PropertyChanged += OnMainViewModelPropertyChanged;
            newVm.ApplyShortcuts(this);
            newVm.QuickActionBarViewportWidth = QuickActionsViewport.ActualWidth;
            FocusCommandPaletteSearchBoxIfVisible();
        }
        else
        {
            _boundViewModel = null;
            InputBindings.Clear();
        }
    }

    private void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsCommandPaletteVisible))
            FocusCommandPaletteSearchBoxIfVisible();
    }

    private void FocusCommandPaletteSearchBoxIfVisible()
    {
        if (_boundViewModel?.IsCommandPaletteVisible != true)
            return;

        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (!CommandPaletteSearchBox.IsVisible)
                return;

            CommandPaletteSearchBox.Focus();
            Keyboard.Focus(CommandPaletteSearchBox);
            CommandPaletteSearchBox.SelectAll();
        }));
    }

    private void OnCommandPalettePreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_boundViewModel?.IsCommandPaletteVisible != true)
            return;

        switch (e.Key)
        {
            case Key.Escape:
                _boundViewModel.CloseCommandPaletteCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Down:
                _boundViewModel.SelectNextCommandPaletteItemCommand.Execute(null);
                CommandPaletteResultsList.ScrollIntoView(_boundViewModel.SelectedCommandPaletteItem);
                e.Handled = true;
                break;
            case Key.Up:
                _boundViewModel.SelectPreviousCommandPaletteItemCommand.Execute(null);
                CommandPaletteResultsList.ScrollIntoView(_boundViewModel.SelectedCommandPaletteItem);
                e.Handled = true;
                break;
            case Key.Enter:
                _boundViewModel.ExecuteSelectedCommandPaletteItemCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void OnCommandPaletteResultsMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_boundViewModel?.SelectedCommandPaletteItem is null)
            return;

        _boundViewModel.ExecuteSelectedCommandPaletteItemCommand.Execute(null);
        e.Handled = true;
    }

    private void OnQuickActionsViewportSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_boundViewModel is null)
            return;

        _boundViewModel.QuickActionBarViewportWidth = QuickActionsViewport.ActualWidth;
    }

    private void OnQuickActionOverflowPopupClosed(object sender, EventArgs e)
    {
        if (_boundViewModel is null)
            return;

        _boundViewModel.IsQuickActionOverflowOpen = false;
    }

    private void OnQuickActionOverflowItemClick(object sender, RoutedEventArgs e)
    {
        _boundViewModel?.CloseQuickActionOverflowCommand.Execute(null);
    }

    private void OnMinimizeWindowClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
        e.Handled = true;
    }

    private void OnCloseWindowClick(object sender, RoutedEventArgs e)
    {
        Application.Current?.Shutdown();
        e.Handled = true;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ShellContentHost.ScrollOffsetChanged -= OnShellContentScrollChanged;

        if (_boundViewModel is not null)
        {
            _boundViewModel.RequestClose = null;
            _boundViewModel.PropertyChanged -= OnMainViewModelPropertyChanged;
            _boundViewModel = null;
        }

        try
        {
            (DataContext as IDisposable)?.Dispose();
        }
        catch
        {
            // Closing the shell must still terminate the app even if disposal fails.
        }
        finally
        {
            var app = Application.Current;
            if (app is not null)
            {
                foreach (var window in app.Windows.OfType<Window>().Where(window => window != this).ToList())
                {
                    try
                    {
                        window.Close();
                    }
                    catch
                    {
                        // Best-effort close during app shutdown.
                    }
                }

                if (!app.Dispatcher.HasShutdownStarted && !app.Dispatcher.HasShutdownFinished)
                    app.Shutdown();
            }
        }
    }
}
