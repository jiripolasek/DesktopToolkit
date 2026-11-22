// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class ChartPixelSnap
{
    private readonly FrameworkElement _owner;
    private readonly Action _onScaleChanged;
    private XamlRoot? _root;
    private double _scale;

    public double Scale => this._owner.XamlRoot?.RasterizationScale ?? 1;

    public ChartPixelSnap(FrameworkElement owner, Action onScaleChanged)
    {
        this._owner = owner;
        this._onScaleChanged = onScaleChanged;
        owner.Loaded += this.OnLoaded;
        owner.Unloaded += this.OnUnloaded;
        this.Connect();
    }

    public static double Round(double value, double scale)
    {
        return Math.Round(value * scale) / scale;
    }

    public void Detach()
    {
        this._owner.Loaded -= this.OnLoaded;
        this._owner.Unloaded -= this.OnUnloaded;
        this.Disconnect();
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        var previous = this._scale;
        this.Connect();
        if (this._scale != previous)
        {
            this._onScaleChanged();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        this.Disconnect();
    }

    private void Connect()
    {
        this.Disconnect();
        this._root = this._owner.XamlRoot;
        this._scale = this.Scale;
        if (this._root is not null)
        {
            this._root.Changed += this.OnRootChanged;
        }
    }

    private void Disconnect()
    {
        if (this._root is not null)
        {
            this._root.Changed -= this.OnRootChanged;
            this._root = null;
        }
    }

    private void OnRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        var scale = this.Scale;
        if (this._scale != scale)
        {
            this._scale = scale;
            this._onScaleChanged();
        }
    }
}
