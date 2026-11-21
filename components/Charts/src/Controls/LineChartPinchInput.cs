// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class LineChartPinchInput
{
    private readonly UIElement _surface;
    private readonly Func<Rect> _bounds;
    private readonly Action _beginZoom;
    private readonly Action<double, double> _zoom;
    private readonly ManipulationModes _originalMode;
    private bool _enabled;
    private bool _isPinching;

    public bool IsPinching => this._isPinching;

    public LineChartPinchInput(UIElement surface, Func<Rect> bounds, Action beginZoom, Action<double, double> zoom)
    {
        this._surface = surface;
        this._bounds = bounds;
        this._beginZoom = beginZoom;
        this._zoom = zoom;
        this._originalMode = surface.ManipulationMode;
        surface.ManipulationStarting += this.OnStarting;
        surface.ManipulationDelta += this.OnDelta;
        surface.ManipulationCompleted += this.OnCompleted;
    }

    public void SetEnabled(bool enabled)
    {
        if (this._enabled == enabled)
        {
            return;
        }

        this._enabled = enabled;
        this._isPinching = false;
        this._surface.ManipulationMode = enabled ? this._originalMode | ManipulationModes.Scale : this._originalMode;
    }

    public void Detach()
    {
        this.SetEnabled(false);
        this._surface.ManipulationStarting -= this.OnStarting;
        this._surface.ManipulationDelta -= this.OnDelta;
        this._surface.ManipulationCompleted -= this.OnCompleted;
    }

    private void OnStarting(object sender, ManipulationStartingRoutedEventArgs e)
    {
        e.Container = this._surface;
    }

    private void OnDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        var scale = e.Delta.Scale;
        var bounds = this._bounds();
        if (!this._enabled
            || e.PointerDeviceType != PointerDeviceType.Touch
            || !float.IsFinite(scale)
            || scale <= 0
            || scale == 1
            || bounds.Width <= 0
            || (!this._isPinching && !bounds.Contains(e.Position)))
        {
            return;
        }

        if (!this._isPinching)
        {
            this._isPinching = true;
            this._beginZoom();
        }

        this._zoom(scale, Math.Clamp((e.Position.X - bounds.Left) / bounds.Width, 0, 1));
        e.Handled = true;
    }

    private void OnCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
    {
        this._isPinching = false;
    }
}
