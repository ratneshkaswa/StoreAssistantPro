using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using StoreAssistantPro.Core.Controls;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core;

/// <summary>
/// Base class for all modal dialog windows.  Provides the complete
/// enterprise dialog standard out of the box.
/// </summary>
public abstract class BaseDialogWindow : Window
{
    private bool _overflowHostApplied;

    protected abstract double DialogWidth { get; }
    protected abstract double DialogHeight { get; }

    /// <summary>
    /// Minimum width after the dialog is clamped to the current work area.
    /// Leave at 0 to keep the configured width as the effective minimum.
    /// </summary>
    protected virtual double DialogMinWidth => 0;

    /// <summary>
    /// Minimum height after the dialog is clamped to the current work area.
    /// Leave at 0 to keep the configured height as the effective minimum.
    /// </summary>
    protected virtual double DialogMinHeight => 0;

    /// <summary>
    /// Allows rich settings dialogs to resize while preserving the app-wide
    /// centered modal behavior. Defaults to fixed-size dialogs.
    /// </summary>
    protected virtual bool AllowResize => false;

    /// <summary>
    /// Wraps dialog content in a shared scroll host so inline InfoBars and
    /// validation surfaces never push the shell off-screen.
    /// </summary>
    protected virtual bool EnableOverflowScrollHost => true;

    public static readonly DependencyProperty ConfirmCommandProperty =
        DependencyProperty.Register(
            nameof(ConfirmCommand),
            typeof(ICommand),
            typeof(BaseDialogWindow),
            new PropertyMetadata(null, OnConfirmCommandChanged));

    public static readonly DependencyProperty ConfirmCommandParameterProperty =
        DependencyProperty.Register(
            nameof(ConfirmCommandParameter),
            typeof(object),
            typeof(BaseDialogWindow),
            new PropertyMetadata(null, OnConfirmCommandParameterChanged));

    public ICommand? ConfirmCommand
    {
        get => (ICommand?)GetValue(ConfirmCommandProperty);
        set => SetValue(ConfirmCommandProperty, value);
    }

    public object? ConfirmCommandParameter
    {
        get => GetValue(ConfirmCommandParameterProperty);
        set => SetValue(ConfirmCommandParameterProperty, value);
    }

    protected virtual bool CloseOnEscape => true;

    public BaseDialogWindow(IWindowSizingService sizingService)
    {
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        sizingService.ConfigureDialogWindow(this, DialogWidth, DialogHeight);

        var workArea = SystemParameters.WorkArea;
        MaxWidth = workArea.Width - 16;
        MaxHeight = workArea.Height - 16;
        MinWidth = Math.Min(
            DialogMinWidth > 0 ? DialogMinWidth : Width,
            Width);
        MinHeight = Math.Min(
            DialogMinHeight > 0 ? DialogMinHeight : Height,
            Height);
        ResizeMode = AllowResize ? ResizeMode.CanResizeWithGrip : ResizeMode.NoResize;

        WindowIconHelper.Apply(this);

        SourceInitialized += (_, _) => Win11Backdrop.ApplyDialog(this);
        Loaded += (_, _) => EnsureOverflowScrollHost();

        if (CloseOnEscape)
            KeyboardNav.SetEscapeCommand(this, new CloseDialogCommand(this));
    }

    private static void OnConfirmCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BaseDialogWindow window)
            KeyboardNav.SetDefaultCommand(window, (ICommand?)e.NewValue);
    }

    private static void OnConfirmCommandParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BaseDialogWindow window)
            KeyboardNav.SetDefaultCommandParameter(window, e.NewValue);
    }

    private void EnsureOverflowScrollHost()
    {
        if (_overflowHostApplied
            || !EnableOverflowScrollHost
            || Content is not UIElement content
            || Content is ScrollViewer)
        {
            return;
        }

        _overflowHostApplied = true;
        Content = CreateOverflowHost(content);
    }

    private static ScrollViewer CreateOverflowHost(UIElement content)
    {
        var scrollViewer = new ScrollViewer
        {
            Focusable = false,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            CanContentScroll = false,
            PanningMode = PanningMode.Both
        };

        var viewportHost = new ViewportConstrainedPanel();
        BindingOperations.SetBinding(
            viewportHost,
            ViewportConstrainedPanel.ViewportWidthProperty,
            new Binding(nameof(ScrollViewer.ViewportWidth)) { Source = scrollViewer });
        BindingOperations.SetBinding(
            viewportHost,
            ViewportConstrainedPanel.ViewportHeightProperty,
            new Binding(nameof(ScrollViewer.ViewportHeight)) { Source = scrollViewer });

        viewportHost.Children.Add(content);
        scrollViewer.Content = viewportHost;
        return scrollViewer;
    }

    private sealed class CloseDialogCommand(Window dialog) : ICommand
    {
        public bool CanExecute(object? parameter) => dialog.IsLoaded;

        public void Execute(object? parameter)
        {
            try
            {
                dialog.DialogResult ??= false;
            }
            catch (InvalidOperationException)
            {
                dialog.Close();
            }
        }

        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
