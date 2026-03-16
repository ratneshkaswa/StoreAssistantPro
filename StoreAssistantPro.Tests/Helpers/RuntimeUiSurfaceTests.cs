using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Controls;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Views;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.ViewModels;
using StoreAssistantPro.Modules.MainShell.ViewModels;
using StoreAssistantPro.Modules.MainShell.Views;

namespace StoreAssistantPro.Tests.Helpers;

[Collection("WpfUi")]
public sealed class RuntimeUiSurfaceTests
{
    private static readonly Type[] StartupWindowTypes =
    [
    ];

    private static readonly Type[] DialogWindowTypes =
    [
    ];

    [Fact(Skip = "Exploratory runtime-render audit; run individually when validating full-window visual surfaces.")]
    public void ApplicationWindows_ShouldNotEmit_RuntimeLayoutOrStyleDiagnostics()
    {
        var failures = RunOnStaThread(() =>
        {
            EnsureApplicationResources();

            using var host = BuildHost();
            var failures = new List<string>();
            Window? keepAliveWindow = null;

            try
            {
                keepAliveWindow = CreateKeepAliveWindow();

                foreach (var windowType in StartupWindowTypes)
                    AuditRuntimeDiagnostics(host.Services, windowType, failures);

                AuditRuntimeDiagnostics(host.Services, typeof(MainWindow), failures);
                ClearDialogOwner(host.Services);

                foreach (var windowType in DialogWindowTypes)
                    AuditRuntimeDiagnostics(host.Services, windowType, failures);
            }
            finally
            {
                CloseWindow(keepAliveWindow);
                BaseViewModel.SetLoggerFactory(NullLoggerFactory.Instance);
            }

            return failures;
        });

        Assert.True(
            failures.Count == 0,
            $"Runtime UI diagnostics audit failed:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    [Fact(Skip = "Exploratory runtime-render audit; run individually when validating full-window visual surfaces.")]
    public void ApplicationWindows_ShouldExpose_VisibleInteractiveSurface_WhenLoaded()
    {
        var failures = RunOnStaThread(() =>
        {
            EnsureApplicationResources();

            using var host = BuildHost();
            var failures = new List<string>();
            Window? keepAliveWindow = null;

            try
            {
                keepAliveWindow = CreateKeepAliveWindow();

                foreach (var windowType in StartupWindowTypes)
                    AuditVisibleSurface(host.Services, windowType, failures);

                AuditVisibleSurface(host.Services, typeof(MainWindow), failures);
                ClearDialogOwner(host.Services);

                foreach (var windowType in DialogWindowTypes)
                    AuditVisibleSurface(host.Services, windowType, failures);
            }
            finally
            {
                CloseWindow(keepAliveWindow);
                BaseViewModel.SetLoggerFactory(NullLoggerFactory.Instance);
            }

            return failures;
        });

        Assert.True(
            failures.Count == 0,
            $"Runtime visible-surface audit failed:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    [Fact(Skip = "Exploratory runtime-render audit; run individually when validating full-window visual surfaces.")]
    public void ApplicationWindows_ShouldRender_NonBlankBitmapSurfaces()
    {
        var failures = RunOnStaThread(() =>
        {
            EnsureApplicationResources();

            using var host = BuildHost();
            var failures = new List<string>();
            Window? keepAliveWindow = null;

            try
            {
                keepAliveWindow = CreateKeepAliveWindow();

                foreach (var windowType in StartupWindowTypes)
                    AuditBitmapSurface(host.Services, windowType, failures);

                AuditBitmapSurface(host.Services, typeof(MainWindow), failures);
                ClearDialogOwner(host.Services);

                foreach (var windowType in DialogWindowTypes)
                    AuditBitmapSurface(host.Services, windowType, failures);
            }
            finally
            {
                CloseWindow(keepAliveWindow);
                BaseViewModel.SetLoggerFactory(NullLoggerFactory.Instance);
            }

            return failures;
        });

        Assert.True(
            failures.Count == 0,
            $"Runtime bitmap-render audit failed:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    [Fact(Skip = "Stateful runtime popup audit; run individually when validating transient UI surfaces.")]
    public void MainWindow_ShouldRender_NotificationPopup_And_ToastOverlay_WhenActive()
    {
        var failures = RunOnStaThread(() =>
        {
            EnsureApplicationResources();

            using var host = BuildHost();
            var failures = new List<string>();
            Window? keepAliveWindow = null;
            MainWindow? window = null;

            try
            {
                keepAliveWindow = CreateKeepAliveWindow();
                window = (MainWindow)ResolveWindow(host.Services, typeof(MainWindow));

                if (window.DataContext is not MainViewModel viewModel)
                {
                    viewModel = host.Services.GetRequiredService<MainViewModel>();
                    window.DataContext = viewModel;
                }

                PrepareWindowForAudit(window);
                window.Show();
                DrainDispatcher();
                window.UpdateLayout();

                if (window.Content is FrameworkElement root)
                    EnsureFiniteLayout(window, root);

                viewModel.AppState.AddNotification(new AppNotification
                {
                    Title = "Inventory sync warning",
                    Message = "A newly added notification must stay visible inside the transient popup audit surface.",
                    Timestamp = DateTime.UtcNow,
                    Level = AppNotificationLevel.Warning
                });

                viewModel.AppState.AddNotification(new AppNotification
                {
                    Title = "Protected action blocked",
                    Message = "This audit message keeps the popup populated with multiple notification rows.",
                    Timestamp = DateTime.UtcNow.AddMinutes(-1),
                    Level = AppNotificationLevel.Error
                });

                viewModel.ToastService.Show(
                    "Transient toast rendering is active during the runtime audit.",
                    AppNotificationLevel.Warning,
                    TimeSpan.FromMinutes(1));

                viewModel.IsNotificationsPanelVisible = true;
                DrainDispatcher();
                window.UpdateLayout();

                var popup = FindNamedElement<Popup>(window, "NotificationsPopup");
                if (popup is null)
                {
                    failures.Add("MainWindow: NotificationsPopup was not registered in the window namescope.");
                }
                else
                {
                    popup.StaysOpen = true;

                    if (!popup.IsOpen)
                    {
                        popup.IsOpen = true;
                        DrainDispatcher();
                        window.UpdateLayout();
                    }

                    var panel = popup.Child as FrameworkElement
                        ?? FindNamedElement<FrameworkElement>(window, "NotificationsPanel");

                    if (panel is null)
                    {
                        failures.Add("MainWindow: notifications popup did not expose a renderable panel child.");
                    }
                    else
                    {
                        EnsureFiniteLayout(panel, 320, 240);

                        var surface = CollectElementSurfaceMetrics(panel);
                        var bitmap = CollectElementBitmapMetrics(panel);

                        if (surface.VisibleSurfaceElements < 4)
                        {
                            failures.Add(
                                $"MainWindow: notifications popup rendered only {surface.VisibleSurfaceElements} visible surface elements.");
                        }

                        if (surface.VisibleInteractiveElements < 1)
                        {
                            failures.Add("MainWindow: notifications popup rendered without any visible interactive controls.");
                        }

                        if (window.ActualWidth > 0 && panel.ActualWidth > window.ActualWidth + 1)
                        {
                            failures.Add(
                                $"MainWindow: notifications popup widened to {panel.ActualWidth:F0}px inside a {window.ActualWidth:F0}px shell.");
                        }

                        var minimumDifferentPixels = Math.Max(1500, bitmap.TotalPixels / 650);
                        if (bitmap.DifferentPixels < minimumDifferentPixels)
                        {
                            failures.Add(
                                $"MainWindow: notifications popup rendered only {bitmap.DifferentPixels} non-background pixels; expected at least {minimumDifferentPixels}.");
                        }
                    }
                }

                var toastHost = EnumerateTree(window).OfType<ToastHost>().SingleOrDefault();
                if (toastHost is null)
                {
                    failures.Add("MainWindow: toast overlay host was not present in the visual tree.");
                }
                else
                {
                    toastHost.ApplyTemplate();
                    DrainDispatcher();
                    EnsureFiniteLayout(toastHost, 360, 160);

                    if (!ReferenceEquals(toastHost.Toasts, viewModel.ToastService.Toasts))
                    {
                        failures.Add("MainWindow: toast overlay is not bound to MainViewModel.ToastService.Toasts.");
                    }

                    if (toastHost.Toasts is null || toastHost.Toasts.Count == 0)
                    {
                        failures.Add("MainWindow: toast overlay did not receive the active toast collection.");
                    }

                    var surface = CollectElementSurfaceMetrics(toastHost);
                    var bitmap = CollectElementBitmapMetrics(ResolveBitmapRenderRoot(toastHost));

                    if (surface.VisibleSurfaceElements < 1)
                    {
                        failures.Add("MainWindow: toast overlay rendered without any visible surface content.");
                    }

                    var minimumDifferentPixels = Math.Max(1000, bitmap.TotalPixels / 750);
                    if (bitmap.DifferentPixels < minimumDifferentPixels)
                    {
                        failures.Add(
                            $"MainWindow: toast overlay rendered only {bitmap.DifferentPixels} non-background pixels; expected at least {minimumDifferentPixels}.");
                    }
                }
            }
            finally
            {
                if (window?.DataContext is MainViewModel viewModel)
                    viewModel.IsNotificationsPanelVisible = false;

                CloseWindow(window);
                CloseWindow(keepAliveWindow);
                BaseViewModel.SetLoggerFactory(NullLoggerFactory.Instance);
            }

            return failures;
        });

        Assert.True(
            failures.Count == 0,
            $"Runtime transient main-shell audit failed:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    [Fact(Skip = "Exploratory runtime-render audit; run individually when validating modal dialog visual surfaces.")]
    public void TransientDialogs_ShouldRender_LongPrompt_And_Message_States()
    {
        var failures = RunOnStaThread(() =>
        {
            EnsureApplicationResources();

            using var host = BuildHost();
            var failures = new List<string>();
            Window? keepAliveWindow = null;

            try
            {
                keepAliveWindow = CreateKeepAliveWindow();
                ClearDialogOwner(host.Services);

                var sizingService = host.Services.GetRequiredService<IWindowSizingService>();

                var messageMetrics = ShowWindowAndCollectRenderMetrics(
                    new AppMessageDialog(
                        sizingService,
                        "Critical inventory warning",
                        string.Concat(Enumerable.Repeat("LongRuntimeAuditMessageSegment0123456789 ", 10)),
                        AppMessageDialogKind.Error,
                        "Acknowledge and continue",
                        "Cancel"));

                if (messageMetrics.Surface.VisibleSurfaceElements < 4)
                {
                    failures.Add(
                        $"AppMessageDialog: only {messageMetrics.Surface.VisibleSurfaceElements} visible surface elements were rendered for the long-message state.");
                }

                if (messageMetrics.Surface.VisibleInteractiveElements < 2)
                {
                    failures.Add("AppMessageDialog: long-message state did not render both dialog buttons.");
                }

                var masterPinMetrics = ShowWindowAndCollectRenderMetrics(
                    new MasterPinDialog(
                        sizingService,
                        string.Concat(Enumerable.Repeat(
                            "Administrator approval is required before continuing this protected workflow. ",
                            6))));

                if (masterPinMetrics.Surface.VisibleSurfaceElements < 4)
                {
                    failures.Add(
                        $"MasterPinDialog: only {masterPinMetrics.Surface.VisibleSurfaceElements} visible surface elements were rendered for the long-prompt state.");
                }

                if (masterPinMetrics.Surface.VisibleInteractiveElements < 3)
                {
                    failures.Add("MasterPinDialog: long-prompt state did not render the PIN box and both action buttons.");
                }

                if (masterPinMetrics.Surface.RootHeight < 160)
                {
                    failures.Add(
                        $"MasterPinDialog: root content height was only {masterPinMetrics.Surface.RootHeight:F0}px for the expanded prompt.");
                }

            }
            finally
            {
                CloseWindow(keepAliveWindow);
                BaseViewModel.SetLoggerFactory(NullLoggerFactory.Instance);
            }

            return failures;
        });

        Assert.True(
            failures.Count == 0,
            $"Runtime transient dialog audit failed:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    [Fact(Skip = "Stateful runtime control audit; run individually when validating transient UI surfaces.")]
    public void SharedTransientControls_ShouldRender_ActiveStates()
    {
        var failures = RunOnStaThread(() =>
        {
            EnsureApplicationResources();

            var failures = new List<string>();
            Window? hostWindow = null;

            try
            {
                var infoBar = new InfoBar
                {
                    Title = "Database unavailable",
                    Message = "The runtime audit keeps this InfoBar open to verify active transient rendering.",
                    Severity = InfoBarSeverity.Error,
                    IsClosable = true,
                    IsOpen = true,
                    Margin = new Thickness(0, 0, 0, 20)
                };

                var loadingOverlay = new LoadingOverlay
                {
                    IsActive = true,
                    Message = "Synchronizing catalog data..."
                };

                var emptyStateOverlay = new EmptyStateOverlay
                {
                    Icon = "[]",
                    Title = "No records available",
                    Description = "This empty-state surface remains visible during the runtime audit.",
                    ActionText = "Create record",
                    ActionCommand = ApplicationCommands.Help,
                    ItemCount = 0
                };

                var toastHost = new ToastHost
                {
                    Toasts = new ObservableCollection<ToastItem>
                    {
                        new ToastItem
                        {
                            Message = "Settings saved successfully.",
                            Level = AppNotificationLevel.Success
                        },
                        new ToastItem
                        {
                            Message = "Low stock warning requires manual review.",
                            Level = AppNotificationLevel.Warning
                        }
                    },
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                var loadingSurface = new Grid
                {
                    Height = 180,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                loadingSurface.Children.Add(loadingOverlay);

                var emptySurface = new Grid
                {
                    Height = 220
                };
                emptySurface.Children.Add(emptyStateOverlay);

                var root = new Grid
                {
                    Margin = new Thickness(24)
                };
                root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                Grid.SetRow(infoBar, 0);
                root.Children.Add(infoBar);

                Grid.SetRow(loadingSurface, 1);
                root.Children.Add(loadingSurface);

                Grid.SetRow(emptySurface, 2);
                root.Children.Add(emptySurface);

                Grid.SetRowSpan(toastHost, 3);
                root.Children.Add(toastHost);

                hostWindow = new Window
                {
                    Content = root,
                    Width = 960,
                    Height = 720,
                    ShowActivated = false,
                    ShowInTaskbar = false,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000
                };

                hostWindow.Show();
                DrainDispatcher();
                hostWindow.UpdateLayout();
                EnsureFiniteLayout(hostWindow, root);

                AuditTransientElement("InfoBar", infoBar, 480, 80, 3, 1, 1400, failures);
                AuditTransientElement("LoadingOverlay", loadingOverlay, 720, 160, 1, 0, 800, failures);
                AuditTransientElement("EmptyStateOverlay", emptyStateOverlay, 640, 220, 3, 1, 1200, failures);
                AuditTransientElement("ToastHost", toastHost, 360, 180, 1, 0, 600, failures);
            }
            finally
            {
                CloseWindow(hostWindow);
            }

            return failures;
        });

        Assert.True(
            failures.Count == 0,
            $"Runtime transient control audit failed:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
    }

    private static void AuditRuntimeDiagnostics(IServiceProvider services, Type windowType, List<string> failures)
    {
        try
        {
            var window = ResolveWindow(services, windowType);
            var diagnostics = ShowWindowAndCollectDiagnostics(window);

            foreach (var diagnostic in diagnostics)
            {
                if (!IsExpectedRuntimeDiagnostic(windowType, diagnostic))
                    failures.Add($"{windowType.Name}: {diagnostic}");
            }
        }
        catch (Exception ex)
        {
            failures.Add($"{windowType.Name}: {ex.GetType().Name} - {ex.Message}");
        }
    }

    private static void AuditVisibleSurface(IServiceProvider services, Type windowType, List<string> failures)
    {
        try
        {
            var window = ResolveWindow(services, windowType);
            var metrics = ShowWindowAndCollectSurfaceMetrics(window);
            var minimumVisibleSurface = windowType == typeof(MainWindow) ? 12 : 6;

            if (metrics.VisibleSurfaceElements < minimumVisibleSurface)
            {
                failures.Add(
                    $"{windowType.Name}: only {metrics.VisibleSurfaceElements} visible surface elements were laid out; expected at least {minimumVisibleSurface}.");
            }

            if (metrics.VisibleInteractiveElements == 0)
            {
                failures.Add($"{windowType.Name}: no visible interactive controls were laid out.");
            }

            if (metrics.RootWidth < 240 || metrics.RootHeight < 160)
            {
                failures.Add(
                    $"{windowType.Name}: root content laid out at only {metrics.RootWidth:F0}x{metrics.RootHeight:F0}.");
            }
        }
        catch (Exception ex)
        {
            failures.Add($"{windowType.Name}: {ex.GetType().Name} - {ex.Message}");
        }
    }

    private static void AuditBitmapSurface(IServiceProvider services, Type windowType, List<string> failures)
    {
        try
        {
            var window = ResolveWindow(services, windowType);
            var metrics = ShowWindowAndCollectBitmapMetrics(window);
            var minimumDifferentPixels = Math.Max(2000, metrics.TotalPixels / 500);

            if (metrics.TotalPixels <= 0)
            {
                failures.Add($"{windowType.Name}: produced an empty render target.");
                return;
            }

            if (metrics.DifferentPixels < minimumDifferentPixels)
            {
                failures.Add(
                    $"{windowType.Name}: rendered only {metrics.DifferentPixels} non-background pixels out of {metrics.TotalPixels}; expected at least {minimumDifferentPixels}.");
            }
        }
        catch (Exception ex)
        {
            failures.Add($"{windowType.Name}: {ex.GetType().Name} - {ex.Message}");
        }
    }

    private static IReadOnlyList<string> ShowWindowAndCollectDiagnostics(Window window)
    {
        var listener = new CollectingTraceListener();

        try
        {
            Trace.Listeners.Add(listener);
            LayoutDiagnostics.SetIsEnabled(window, true);
            StyleComplianceDiagnostics.SetIsEnabled(window, true);
            PrepareWindowForAudit(window);

            window.ApplyTemplate();
            _ = window.Content;
            _ = window.DataContext;
            window.Show();
            DrainDispatcher();
            window.UpdateLayout();

            return listener.Messages
                .Where(message =>
                    message.Contains("[LayoutDiagnostics]", StringComparison.Ordinal)
                    || message.Contains("[StyleComplianceDiagnostics]", StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
        finally
        {
            Trace.Listeners.Remove(listener);
            listener.Dispose();
            CloseWindow(window);
        }
    }

    private static bool IsExpectedRuntimeDiagnostic(Type windowType, string diagnostic)
    {
        if (diagnostic.Contains("ScrollViewer at depth", StringComparison.Ordinal)
            && diagnostic.Contains("wraps only form controls", StringComparison.Ordinal))
        {
            return false;
        }

        if (diagnostic.Contains("Window.Content is a ScrollViewer", StringComparison.Ordinal))
            return DialogWindowTypes.Contains(windowType);

        return false;
    }

    private static SurfaceMetrics ShowWindowAndCollectSurfaceMetrics(Window window)
    {
        try
        {
            PrepareWindowForAudit(window);

            window.ApplyTemplate();
            _ = window.Content;
            _ = window.DataContext;
            window.Show();
            DrainDispatcher();
            window.UpdateLayout();
            DrainDispatcher(DispatcherPriority.Render);
            window.UpdateLayout();

            var root = window.Content as FrameworkElement;
            if (root is not null)
                EnsureFiniteLayout(window, root);

            return root is null
                ? new SurfaceMetrics(0, 0, 0, 0)
                : CollectElementSurfaceMetrics(root);
        }
        finally
        {
            CloseWindow(window);
        }
    }

    private static BitmapMetrics ShowWindowAndCollectBitmapMetrics(Window window)
    {
        try
        {
            PrepareWindowForAudit(window);

            window.ApplyTemplate();
            _ = window.Content;
            _ = window.DataContext;
            window.Show();
            DrainDispatcher();
            window.UpdateLayout();
            DrainDispatcher(DispatcherPriority.Render);
            window.UpdateLayout();

            if (window.Content is not FrameworkElement root)
                return new BitmapMetrics(0, 0);

            EnsureFiniteLayout(window, root);

            return CollectElementBitmapMetrics(root);
        }
        finally
        {
            CloseWindow(window);
        }
    }

    private static WindowRenderMetrics ShowWindowAndCollectRenderMetrics(Window window)
    {
        try
        {
            PrepareWindowForAudit(window);

            window.ApplyTemplate();
            _ = window.Content;
            _ = window.DataContext;
            window.Show();
            DrainDispatcher();
            window.UpdateLayout();
            DrainDispatcher(DispatcherPriority.Render);
            window.UpdateLayout();

            if (window.Content is not FrameworkElement root)
            {
                return new WindowRenderMetrics(
                    window.ActualWidth,
                    window.ActualHeight,
                    new SurfaceMetrics(0, 0, 0, 0),
                    new BitmapMetrics(0, 0));
            }

            EnsureFiniteLayout(window, root);
            var renderRoot = ResolveBitmapRenderRoot(root);
            EnsureFiniteLayout(
                renderRoot,
                Math.Max(window.ActualWidth, 360),
                Math.Max(window.ActualHeight, 180));

            return new WindowRenderMetrics(
                window.ActualWidth,
                window.ActualHeight,
                CollectElementSurfaceMetrics(renderRoot),
                CollectElementBitmapMetrics(renderRoot));
        }
        finally
        {
            CloseWindow(window);
        }
    }

    private static bool IsVisibleSurfaceElement(FrameworkElement element) =>
        element.Visibility == Visibility.Visible
        && element.Opacity > 0
        && element.ActualWidth >= 8
        && element.ActualHeight >= 8
        && element is ButtonBase
            or TextBox
            or PasswordBox
            or ComboBox
            or DatePicker
            or CheckBox
            or RadioButton
            or DataGrid
            or ListView
            or ListBox
            or TextBlock
            or TabControl
            or TabItem;

    private static bool IsInteractiveSurfaceElement(FrameworkElement element) =>
        element is ButtonBase
            or TextBox
            or PasswordBox
            or ComboBox
            or DatePicker
            or CheckBox
            or RadioButton
            or DataGrid
            or ListView
            or ListBox
            or TabControl
            or TabItem;

    private static SurfaceMetrics CollectElementSurfaceMetrics(FrameworkElement root)
    {
        var surfaceElements = EnumerateTree(root)
            .OfType<FrameworkElement>()
            .Where(IsVisibleSurfaceElement)
            .ToArray();

        var visibleInteractiveElements = surfaceElements.Count(IsInteractiveSurfaceElement);

        return new SurfaceMetrics(
            root.ActualWidth,
            root.ActualHeight,
            surfaceElements.Length,
            visibleInteractiveElements);
    }

    private static BitmapMetrics CollectElementBitmapMetrics(FrameworkElement root)
    {
        var pixelWidth = Math.Max(1, (int)Math.Ceiling(root.ActualWidth));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(root.ActualHeight));
        var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);

        var stride = pixelWidth * 4;
        var pixels = new byte[stride * pixelHeight];
        bitmap.CopyPixels(pixels, stride, 0);

        var baseB = pixels[0];
        var baseG = pixels[1];
        var baseR = pixels[2];
        var baseA = pixels[3];
        var differentPixels = 0;

        for (var offset = 0; offset < pixels.Length; offset += 4)
        {
            var b = pixels[offset];
            var g = pixels[offset + 1];
            var r = pixels[offset + 2];
            var a = pixels[offset + 3];

            if (Math.Abs(b - baseB) > 8
                || Math.Abs(g - baseG) > 8
                || Math.Abs(r - baseR) > 8
                || Math.Abs(a - baseA) > 8)
            {
                differentPixels++;
            }
        }

        return new BitmapMetrics(pixelWidth * pixelHeight, differentPixels);
    }

    private static FrameworkElement ResolveBitmapRenderRoot(FrameworkElement root)
    {
        var descendant = EnumerateTree(root)
            .OfType<FrameworkElement>()
            .Where(element =>
                !ReferenceEquals(element, root)
                && element.Visibility == Visibility.Visible
                && element.ActualWidth >= 8
                && element.ActualHeight >= 8)
            .OrderByDescending(element => element.ActualWidth * element.ActualHeight)
            .FirstOrDefault();

        return descendant ?? root;
    }

    private static void EnsureFiniteLayout(Window window, FrameworkElement root)
    {
        var width = window.ActualWidth > 0
            ? window.ActualWidth
            : window.Width > 0
                ? window.Width
                : window is MainWindow
                    ? 1440
                    : 1200;

        var height = window.ActualHeight > 0
            ? window.ActualHeight
            : window.Height > 0
                ? window.Height
                : window is MainWindow
                    ? 900
                    : 800;

        root.Measure(new Size(width, height));
        root.Arrange(new Rect(0, 0, width, height));
        root.UpdateLayout();
        DrainDispatcher();
    }

    private static void EnsureFiniteLayout(FrameworkElement root, double fallbackWidth, double fallbackHeight)
    {
        var width = root.ActualWidth > 0
            ? root.ActualWidth
            : !double.IsNaN(root.Width) && root.Width > 0
                ? root.Width
                : root.DesiredSize.Width > 0
                    ? root.DesiredSize.Width
                    : fallbackWidth;

        var height = root.ActualHeight > 0
            ? root.ActualHeight
            : !double.IsNaN(root.Height) && root.Height > 0
                ? root.Height
                : root.DesiredSize.Height > 0
                    ? root.DesiredSize.Height
                    : fallbackHeight;

        root.Measure(new Size(width, height));
        root.Arrange(new Rect(0, 0, width, height));
        root.UpdateLayout();
        DrainDispatcher();
    }

    private static T? FindNamedElement<T>(FrameworkElement root, string name)
        where T : class =>
        root.FindName(name) as T;

    private static void AuditTransientElement(
        string elementName,
        FrameworkElement element,
        double fallbackWidth,
        double fallbackHeight,
        int minimumVisibleSurface,
        int minimumInteractiveSurface,
        int minimumDifferentPixels,
        List<string> failures)
    {
        try
        {
            element.ApplyTemplate();
            EnsureFiniteLayout(element, fallbackWidth, fallbackHeight);

            var surface = CollectElementSurfaceMetrics(element);
            var bitmap = CollectElementBitmapMetrics(ResolveBitmapRenderRoot(element));

            if (surface.VisibleSurfaceElements < minimumVisibleSurface)
            {
                failures.Add(
                    $"{elementName}: only {surface.VisibleSurfaceElements} visible surface elements were rendered.");
            }

            if (minimumInteractiveSurface > 0 && surface.VisibleInteractiveElements < minimumInteractiveSurface)
            {
                failures.Add(
                    $"{elementName}: only {surface.VisibleInteractiveElements} visible interactive elements were rendered.");
            }

            var pixelThreshold = Math.Max(minimumDifferentPixels, bitmap.TotalPixels / 800);
            if (bitmap.DifferentPixels < pixelThreshold)
            {
                failures.Add(
                    $"{elementName}: rendered only {bitmap.DifferentPixels} non-background pixels; expected at least {pixelThreshold}.");
            }
        }
        catch (Exception ex)
        {
            failures.Add($"{elementName}: {ex.GetType().Name} - {ex.Message}");
        }
    }

    private static Window ResolveWindow(IServiceProvider services, Type windowType)
    {
        var window = (Window)services.GetRequiredService(windowType);

        if (window is MainWindow mainWindow && mainWindow.DataContext is null)
            mainWindow.DataContext = services.GetRequiredService<MainViewModel>();

        return window;
    }

    private static void PrepareWindowForAudit(Window window)
    {
        window.ShowActivated = false;
        window.ShowInTaskbar = false;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -10000;
        window.Top = -10000;
    }

    private static void PrepareWindowForClose(Window window)
    {
        switch (window.DataContext)
        {
            case LoginViewModel loginViewModel:
                loginViewModel.IsVerifying = false;
                loginViewModel.IsBusy = false;
                break;
        }
    }

    private static void CloseWindow(Window? window)
    {
        if (window is null)
            return;

        PrepareWindowForClose(window);

        if (window.IsVisible)
            window.Close();

        DrainDispatcher();
    }

    private static Window CreateKeepAliveWindow()
    {
        var window = new Window
        {
            Width = 1,
            Height = 1,
            ShowActivated = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            WindowStyle = WindowStyle.None,
            Left = -20000,
            Top = -20000,
            Opacity = 0
        };

        window.Show();
        DrainDispatcher();
        return window;
    }

    private static void ClearDialogOwner(IServiceProvider services)
    {
        var sizingService = services.GetRequiredService<IWindowSizingService>();
        var field = sizingService.GetType().GetField("_mainWindow", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate WindowSizingService._mainWindow.");

        field.SetValue(sizingService, null);
    }

    private static IEnumerable<DependencyObject> EnumerateTree(DependencyObject root)
    {
        var stack = new Stack<DependencyObject>();
        var visited = new HashSet<DependencyObject>(ReferenceEqualityComparer.Instance);
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
                continue;

            yield return current;

            if (current is Popup popup && popup.Child is not null)
                stack.Push(popup.Child);

            foreach (var child in LogicalTreeHelper.GetChildren(current).OfType<DependencyObject>())
                stack.Push(child);

            if (current is Visual or Visual3D)
            {
                for (var i = VisualTreeHelper.GetChildrenCount(current) - 1; i >= 0; i--)
                    stack.Push(VisualTreeHelper.GetChild(current, i));
            }
        }
    }

    private static void DrainDispatcher(DispatcherPriority priority = DispatcherPriority.Input)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Send, Dispatcher.CurrentDispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();
            frame.Continue = false;
        };

        Dispatcher.CurrentDispatcher.BeginInvoke(
            priority,
            new DispatcherOperationCallback(_ =>
            {
                timer.Stop();
                frame.Continue = false;
                return null;
            }),
            null);

        timer.Start();
        Dispatcher.PushFrame(frame);
    }

    private static IHost BuildHost()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = FindContentRoot()
        });

        InvokeHostingExtension("AddDataAccess", builder.Services, builder.Configuration);
        InvokeHostingExtension("AddCoreServices", builder.Services);
        InvokeHostingExtension("AddModules", builder.Services);

        var host = builder.Build();
        host.Services
            .GetRequiredService<NavigationPageRegistry>()
            .ApplyTo(host.Services.GetRequiredService<INavigationService>());
        InvokeHostingExtension("ApplyDialogRegistrations", host.Services);
        BaseViewModel.SetLoggerFactory(host.Services.GetRequiredService<ILoggerFactory>());
        return host;
    }

    private static object? InvokeHostingExtension(string methodName, params object[] arguments)
    {
        var hostingExtensions = typeof(App).Assembly.GetType("StoreAssistantPro.HostingExtensions")
            ?? throw new InvalidOperationException("Could not locate HostingExtensions.");

        var method = hostingExtensions
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .SingleOrDefault(candidate =>
                candidate.Name == methodName
                && candidate.GetParameters().Length == arguments.Length)
            ?? throw new InvalidOperationException($"Could not locate HostingExtensions.{methodName}.");

        return method.Invoke(null, arguments);
    }

    private static string FindContentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "StoreAssistantPro.slnx"))
                && File.Exists(Path.Combine(directory.FullName, "appsettings.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the StoreAssistantPro content root.");
    }

    private static void EnsureApplicationResources()
        => WpfTestApplication.EnsureStoreAssistantApplication();

    private static T RunOnStaThread<T>(Func<T> action)
        => WpfTestApplication.Run(action);

    private sealed record SurfaceMetrics(
        double RootWidth,
        double RootHeight,
        int VisibleSurfaceElements,
        int VisibleInteractiveElements);

    private sealed record BitmapMetrics(
        int TotalPixels,
        int DifferentPixels);

    private sealed record WindowRenderMetrics(
        double WindowWidth,
        double WindowHeight,
        SurfaceMetrics Surface,
        BitmapMetrics Bitmap);

    private sealed class CollectingTraceListener : TraceListener
    {
        private readonly List<string> _messages = [];
        private readonly StringBuilder _pending = new();

        public IReadOnlyList<string> Messages => _messages;

        public override void Write(string? message)
        {
            if (!string.IsNullOrEmpty(message))
                _pending.Append(message);
        }

        public override void WriteLine(string? message)
        {
            if (!string.IsNullOrEmpty(message))
                _pending.Append(message);

            if (_pending.Length == 0)
                return;

            _messages.Add(_pending.ToString());
            _pending.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _pending.Length > 0)
            {
                _messages.Add(_pending.ToString());
                _pending.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
