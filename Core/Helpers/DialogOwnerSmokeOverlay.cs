using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Shows a neutral Win11-style smoke overlay on an owner window while a modal dialog is open.
/// </summary>
public static class DialogOwnerSmokeOverlay
{
    public static IDisposable? Show(Window owner, Brush overlayBrush)
    {
        if (owner.Content is not UIElement ownerContent)
            return null;

        var adornerLayer = AdornerLayer.GetAdornerLayer(ownerContent);
        if (adornerLayer is null)
            return null;

        var adorner = new SmokeOverlayAdorner(ownerContent, overlayBrush);
        adornerLayer.Add(adorner);
        return new OverlayHandle(adornerLayer, adorner);
    }

    private sealed class OverlayHandle(AdornerLayer layer, Adorner adorner) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            layer.Remove(adorner);
            _disposed = true;
        }
    }

    private sealed class SmokeOverlayAdorner : Adorner
    {
        private readonly Brush _overlayBrush;

        public SmokeOverlayAdorner(UIElement adornedElement, Brush overlayBrush)
            : base(adornedElement)
        {
            IsHitTestVisible = false;
            _overlayBrush = overlayBrush.CloneCurrentValue();
            if (_overlayBrush.CanFreeze)
                _overlayBrush.Freeze();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(
                _overlayBrush,
                null,
                new Rect(AdornedElement.RenderSize));
        }
    }
}
