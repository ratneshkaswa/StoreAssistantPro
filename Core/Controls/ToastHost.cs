using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// Overlay control that renders toast notifications in the bottom-right
/// corner of the window. Bind <see cref="Toasts"/> to the
/// <see cref="Services.IToastService.Toasts"/> collection.
/// <para>
/// <b>Placement:</b> Place as the last child in the MainWindow root
/// <c>Grid</c> so it overlays all other content. The control uses
/// <c>HorizontalAlignment="Right"</c> and
/// <c>VerticalAlignment="Bottom"</c> to anchor bottom-right.
/// </para>
/// <para>
/// <b>Visual layout:</b>
/// <code>
///                                     ┌─────────────────────┐
///                                     │ ✓  Product saved     │
///                                     ├─────────────────────┤
///                                     │ ⚠  Low stock alert   │
///                                     └─────────────────────┘
/// </code>
/// </para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;controls:ToastHost
///     Grid.Row="0" Grid.RowSpan="4"
///     Toasts="{Binding ToastService.Toasts}"/&gt;
/// </code>
/// </summary>
public class ToastHost : Control
{
    // ── Toasts DP ─────────────────────────────────────────────────

    /// <summary>
    /// Observable collection of <see cref="Services.ToastItem"/> to render.
    /// Bind to <c>IToastService.Toasts</c>.
    /// </summary>
    public static readonly DependencyProperty ToastsProperty =
        DependencyProperty.Register(
            nameof(Toasts), typeof(ObservableCollection<Services.ToastItem>),
            typeof(ToastHost),
            new PropertyMetadata(null));

    public ObservableCollection<Services.ToastItem>? Toasts
    {
        get => (ObservableCollection<Services.ToastItem>?)GetValue(ToastsProperty);
        set => SetValue(ToastsProperty, value);
    }

    // ── Constructor ───────────────────────────────────────────────

    static ToastHost()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ToastHost),
            new FrameworkPropertyMetadata(typeof(ToastHost)));

        FocusableProperty.OverrideMetadata(
            typeof(ToastHost),
            new FrameworkPropertyMetadata(false));

        IsHitTestVisibleProperty.OverrideMetadata(
            typeof(ToastHost),
            new FrameworkPropertyMetadata(false));
    }
}
