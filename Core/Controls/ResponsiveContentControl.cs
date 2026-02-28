using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Core.Controls;

/// <summary>
/// A <see cref="ContentControl"/> designed for the MainWindow content region
/// that stretches its content to fill available space while enabling vertical
/// scrolling when the content's desired height exceeds the viewport.
/// </summary>
public class ResponsiveContentControl : ContentControl
{
    // ── Template part names ──────────────────────────────────────────
    private const string PartScrollViewer = "PART_ScrollViewer";

    // ── Cached template parts ────────────────────────────────────────
    private ScrollViewer? _scrollViewer;

    // ── Constructor ──────────────────────────────────────────────────

    static ResponsiveContentControl()
    {
        FocusableProperty.OverrideMetadata(
            typeof(ResponsiveContentControl),
            new FrameworkPropertyMetadata(false));
    }

    // ── Template wiring ──────────────────────────────────────────────

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _scrollViewer = GetTemplateChild(PartScrollViewer) as ScrollViewer;
    }

    // ── Content change → scroll to top ───────────────────────────────

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        _scrollViewer?.ScrollToTop();
    }
}
