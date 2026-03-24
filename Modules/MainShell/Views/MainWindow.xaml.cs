using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Modules.MainShell.Views;

public partial class MainWindow : Window
{
    private const double NotificationPanelEdgeMargin = 16;
    private const double NotificationPanelVerticalMargin = 24;
    private const double QuickActionBarAutoHideThreshold = 48;
    private const double QuickActionBarRevealThreshold = 12;
    private MainViewModel? _boundViewModel;
    private bool _isQuickActionBarVisible = true;
    private double _quickActionBarExpandedHeight;

    public MainWindow(IWindowSizingService sizingService)
    {
        InitializeComponent();
        WindowIconHelper.Apply(this);
        sizingService.ConfigureMainWindow(this);

        SourceInitialized += (_, _) => Win11Backdrop.Apply(this);
        Loaded += (_, _) => UpdateNotificationsPopupLayout();
        Loaded += (_, _) => InitializeQuickActionBarChrome();
        Loaded += (_, _) => ApplyNavigationRailWidth(animate: false);
        SizeChanged += (_, _) => UpdateNotificationsPopupLayout();
        LocationChanged += (_, _) => UpdateNotificationsPopupLayout();
        NotificationBellButton.SizeChanged += (_, _) => UpdateNotificationsPopupLayout();
        ShellContentHost.ScrollOffsetChanged += OnShellContentScrollChanged;
        QuickActionBarHost.SizeChanged += OnQuickActionBarHostSizeChanged;

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

    private void InitializeQuickActionBarChrome()
    {
        if (QuickActionBarHost.Visibility != Visibility.Visible)
            return;

        CaptureQuickActionBarExpandedHeight();
        QuickActionBarHost.MaxHeight = _quickActionBarExpandedHeight > 0
            ? _quickActionBarExpandedHeight
            : double.PositiveInfinity;
        QuickActionBarHost.Opacity = 1;
        QuickActionBarTransform.Y = 0;
        QuickActionBarShadow.Opacity = 0;
        _isQuickActionBarVisible = true;
    }

    private void OnQuickActionBarHostSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_isQuickActionBarVisible || QuickActionBarHost.Visibility != Visibility.Visible)
            return;

        CaptureQuickActionBarExpandedHeight();
        if (_quickActionBarExpandedHeight > 0)
            QuickActionBarHost.MaxHeight = _quickActionBarExpandedHeight;
    }

    private void OnShellContentScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        UpdateQuickActionBarShadow(e.VerticalOffset > 0.5);

        if (QuickActionBarHost.Visibility != Visibility.Visible)
            return;

        if (e.VerticalOffset <= 0.5)
        {
            SetQuickActionBarVisible(true);
            return;
        }

        if (e.VerticalChange > 1 && e.VerticalOffset > QuickActionBarAutoHideThreshold)
        {
            SetQuickActionBarVisible(false);
            return;
        }

        if (e.VerticalChange < -1 || e.VerticalOffset < QuickActionBarRevealThreshold)
            SetQuickActionBarVisible(true);
    }

    private void CaptureQuickActionBarExpandedHeight()
    {
        if (QuickActionBarHost.ActualHeight > 0)
            _quickActionBarExpandedHeight = QuickActionBarHost.ActualHeight;
    }

    private void UpdateQuickActionBarShadow(bool showShadow)
    {
        var targetOpacity = showShadow ? 1d : 0d;
        if (Math.Abs(QuickActionBarShadow.Opacity - targetOpacity) < 0.01)
            return;

        var animation = new DoubleAnimation(targetOpacity, TimeSpan.FromMilliseconds(120))
        {
            EasingFunction = FindResource("FluentEaseDecelerate") as IEasingFunction
        };
        QuickActionBarShadow.BeginAnimation(OpacityProperty, animation);
    }

    private void SetQuickActionBarVisible(bool visible)
    {
        if (_isQuickActionBarVisible == visible || QuickActionBarHost.Visibility != Visibility.Visible)
            return;

        CaptureQuickActionBarExpandedHeight();

        var targetHeight = visible
            ? Math.Max(_quickActionBarExpandedHeight, QuickActionBarHost.ActualHeight)
            : 0d;
        var targetOpacity = visible ? 1d : 0d;
        var targetOffset = visible ? 0d : -8d;
        var duration = TimeSpan.FromMilliseconds(150);
        var ease = FindResource("FluentEaseDecelerate") as IEasingFunction;

        QuickActionBarHost.BeginAnimation(FrameworkElement.MaxHeightProperty, null);
        QuickActionBarHost.MaxHeight = Math.Max(0, QuickActionBarHost.ActualHeight);
        QuickActionBarHost.BeginAnimation(
            FrameworkElement.MaxHeightProperty,
            new DoubleAnimation(targetHeight, duration) { EasingFunction = ease });

        QuickActionBarHost.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(targetOpacity, duration) { EasingFunction = ease });

        QuickActionBarTransform.BeginAnimation(
            System.Windows.Media.TranslateTransform.YProperty,
            new DoubleAnimation(targetOffset, duration) { EasingFunction = ease });

        _isQuickActionBarVisible = visible;
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
            ApplyNavigationRailWidth(animate: false);
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

        if (e.PropertyName == nameof(MainViewModel.IsNavigationRailExpanded))
            ApplyNavigationRailWidth(animate: true);
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

    private void ApplyNavigationRailWidth(bool animate)
    {
        var targetWidth = _boundViewModel?.NavigationRailWidth ?? 56d;
        if (!animate)
        {
            NavigationRailHost.BeginAnimation(FrameworkElement.WidthProperty, null);
            NavigationRailHost.Width = targetWidth;
            return;
        }

        var animation = new DoubleAnimation(targetWidth, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = FindResource("FluentEaseDecelerate") as IEasingFunction
        };
        NavigationRailHost.BeginAnimation(FrameworkElement.WidthProperty, animation);
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

    private void OnClosed(object? sender, EventArgs e)
    {
        ShellContentHost.ScrollOffsetChanged -= OnShellContentScrollChanged;
        QuickActionBarHost.SizeChanged -= OnQuickActionBarHostSizeChanged;

        if (_boundViewModel is not null)
        {
            _boundViewModel.RequestClose = null;
            _boundViewModel.PropertyChanged -= OnMainViewModelPropertyChanged;
            _boundViewModel = null;
        }

        (DataContext as IDisposable)?.Dispose();

        // Single-window architecture: closing the main window shuts down the app
        Application.Current?.Shutdown();
    }
}
