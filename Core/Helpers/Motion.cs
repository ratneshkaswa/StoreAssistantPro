using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Retains the shared attached-property contract for historical XAML usage,
/// but speed-first mode disables all decorative motion behaviors.
/// </summary>
public static class Motion
{
    public static readonly DependencyProperty FadeInProperty =
        DependencyProperty.RegisterAttached(
            "FadeIn", typeof(bool), typeof(Motion),
            new PropertyMetadata(false, OnNoOpChanged));

    public static readonly DependencyProperty FadeOutProperty =
        DependencyProperty.RegisterAttached(
            "FadeOut", typeof(bool), typeof(Motion),
            new PropertyMetadata(false, OnNoOpChanged));

    public static readonly DependencyProperty ScaleHoverProperty =
        DependencyProperty.RegisterAttached(
            "ScaleHover", typeof(bool), typeof(Motion),
            new PropertyMetadata(false, OnNoOpChanged));

    public static readonly DependencyProperty SlideFadeInProperty =
        DependencyProperty.RegisterAttached(
            "SlideFadeIn", typeof(bool), typeof(Motion),
            new PropertyMetadata(false, OnNoOpChanged));

    public static readonly DependencyProperty PageFadeInProperty =
        DependencyProperty.RegisterAttached(
            "PageFadeIn", typeof(bool), typeof(Motion),
            new PropertyMetadata(false, OnNoOpChanged));

    public static readonly DependencyProperty WindowFadeInProperty =
        DependencyProperty.RegisterAttached(
            "WindowFadeIn", typeof(bool), typeof(Motion),
            new PropertyMetadata(false, OnNoOpChanged));

    public static readonly DependencyProperty SlideFadeRevealProperty =
        DependencyProperty.RegisterAttached(
            "SlideFadeReveal", typeof(bool), typeof(Motion),
            new PropertyMetadata(false, OnNoOpChanged));

    public static readonly DependencyProperty StaggerIndexProperty =
        DependencyProperty.RegisterAttached(
            "StaggerIndex", typeof(int), typeof(Motion),
            new PropertyMetadata(-1, OnNoOpChanged));

    public static readonly DependencyProperty StaggerChildrenProperty =
        DependencyProperty.RegisterAttached(
            "StaggerChildren", typeof(bool), typeof(Motion),
            new PropertyMetadata(false, OnNoOpChanged));

    public static readonly DependencyProperty ShakeOnTriggerProperty =
        DependencyProperty.RegisterAttached(
            "ShakeOnTrigger", typeof(bool), typeof(Motion),
            new PropertyMetadata(false, OnShakeOnTriggerChanged));

    public static bool GetFadeIn(DependencyObject obj) => (bool)obj.GetValue(FadeInProperty);
    public static void SetFadeIn(DependencyObject obj, bool value) => obj.SetValue(FadeInProperty, value);

    public static bool GetFadeOut(DependencyObject obj) => (bool)obj.GetValue(FadeOutProperty);
    public static void SetFadeOut(DependencyObject obj, bool value) => obj.SetValue(FadeOutProperty, value);

    public static bool GetScaleHover(DependencyObject obj) => (bool)obj.GetValue(ScaleHoverProperty);
    public static void SetScaleHover(DependencyObject obj, bool value) => obj.SetValue(ScaleHoverProperty, value);

    public static bool GetSlideFadeIn(DependencyObject obj) => (bool)obj.GetValue(SlideFadeInProperty);
    public static void SetSlideFadeIn(DependencyObject obj, bool value) => obj.SetValue(SlideFadeInProperty, value);

    public static bool GetPageFadeIn(DependencyObject obj) => (bool)obj.GetValue(PageFadeInProperty);
    public static void SetPageFadeIn(DependencyObject obj, bool value) => obj.SetValue(PageFadeInProperty, value);

    public static bool GetWindowFadeIn(DependencyObject obj) => (bool)obj.GetValue(WindowFadeInProperty);
    public static void SetWindowFadeIn(DependencyObject obj, bool value) => obj.SetValue(WindowFadeInProperty, value);

    public static bool GetSlideFadeReveal(DependencyObject obj) => (bool)obj.GetValue(SlideFadeRevealProperty);
    public static void SetSlideFadeReveal(DependencyObject obj, bool value) => obj.SetValue(SlideFadeRevealProperty, value);

    public static int GetStaggerIndex(DependencyObject obj) => (int)obj.GetValue(StaggerIndexProperty);
    public static void SetStaggerIndex(DependencyObject obj, int value) => obj.SetValue(StaggerIndexProperty, value);

    public static bool GetStaggerChildren(DependencyObject obj) => (bool)obj.GetValue(StaggerChildrenProperty);
    public static void SetStaggerChildren(DependencyObject obj, bool value) => obj.SetValue(StaggerChildrenProperty, value);

    public static bool GetShakeOnTrigger(DependencyObject obj) => (bool)obj.GetValue(ShakeOnTriggerProperty);
    public static void SetShakeOnTrigger(DependencyObject obj, bool value) => obj.SetValue(ShakeOnTriggerProperty, value);

    private static void OnNoOpChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
    }

    private static void OnShakeOnTriggerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not true)
            return;

        fe.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => fe.SetCurrentValue(ShakeOnTriggerProperty, false)));
    }
}
