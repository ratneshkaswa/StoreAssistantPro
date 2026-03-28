using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Modules.MainShell.Views;

public partial class WorkspaceView : UserControl
{
    private const double ParallaxUpdateThreshold = 0.75;
    private double _lastAppliedParallaxY = double.NaN;

    public WorkspaceView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (StartBillingFab.Visibility == Visibility.Visible)
        {
            _ = Dispatcher.BeginInvoke(
                () => StartBillingFab.Focus(),
                DispatcherPriority.Input);
        }
    }

    private void OnWorkspaceScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        var targetOffset = Math.Min(e.VerticalOffset * 0.5, 56);
        if (!double.IsNaN(_lastAppliedParallaxY)
            && Math.Abs(_lastAppliedParallaxY - targetOffset) < ParallaxUpdateThreshold)
        {
            return;
        }

        HeroBackdropParallaxTransform.Y = targetOffset;
        _lastAppliedParallaxY = targetOffset;
    }
}
