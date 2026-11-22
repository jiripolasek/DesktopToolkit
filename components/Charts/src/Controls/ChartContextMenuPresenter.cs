// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal readonly record struct ChartContextTarget(ChartInspectionItem Inspection, ChartContextValue[] Values);

internal sealed class ChartContextMenuPresenter
{
    private readonly Control _owner;
    private readonly FrameworkElement _plot;
    private readonly Func<Point?, ChartContextTarget?> _read;
    private readonly Action _clearInspection;
    private FlyoutBase? _flyout;

    public bool IsOpen => this._flyout is not null;

    public ChartContextMenuPresenter(Control owner, FrameworkElement plot, Func<Point?, ChartContextTarget?> read, Action clearInspection)
    {
        this._owner = owner;
        this._plot = plot;
        this._read = read;
        this._clearInspection = clearInspection;
        owner.ContextRequested += this.OnContextRequested;
        owner.ContextCanceled += this.OnContextCanceled;
        owner.Unloaded += this.OnUnloaded;
        owner.IsEnabledChanged += this.OnIsEnabledChanged;
    }

    public void Detach()
    {
        this._owner.ContextRequested -= this.OnContextRequested;
        this._owner.ContextCanceled -= this.OnContextCanceled;
        this._owner.Unloaded -= this.OnUnloaded;
        this._owner.IsEnabledChanged -= this.OnIsEnabledChanged;
        this.Close();
    }

    private void OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (args.Handled
            || !this._owner.IsEnabled
            || !this._owner.IsLoaded
            || this._owner.ContextFlyout is not null
            || ChartContextMenu.GetProvider(this._owner) is not { } provider)
        {
            return;
        }

        var isPointer = args.TryGetPosition(this._plot, out var point);
        if (isPointer && !new Rect(0, 0, this._plot.ActualWidth, this._plot.ActualHeight).Contains(point))
        {
            return;
        }

        if (this._read(isPointer ? point : null) is not { } target || target.Values.Length == 0)
        {
            return;
        }

        var position = this._plot.TransformToVisual(this._owner).TransformPoint(isPointer ? point : target.Inspection.Anchor);
        var request = new ChartContextMenuRequest(this._owner, position, isPointer, target.Inspection.Text, target.Values);
        if (provider.CreateFlyout(request) is not { } flyout || !this._owner.IsLoaded)
        {
            return;
        }

        this.Close();
        this._clearInspection();
        this._flyout = flyout;
        flyout.Closed += this.OnFlyoutClosed;
        try
        {
            flyout.ShowAt(this._owner, new FlyoutShowOptions { Position = position, ShowMode = FlyoutShowMode.Standard });
        }
        catch
        {
            this.OnFlyoutClosed(flyout, EventArgs.Empty);
            throw;
        }

        args.Handled = true;
    }

    private void OnContextCanceled(UIElement sender, RoutedEventArgs args)
    {
        this.Close();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        this.Close();
    }

    private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        if (!this._owner.IsEnabled)
        {
            this.Close();
        }
    }

    private void Close()
    {
        if (this._flyout is { } flyout)
        {
            this.OnFlyoutClosed(flyout, EventArgs.Empty);
            flyout.Hide();
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(FlyoutBase))]
    private void OnFlyoutClosed(object? sender, object args)
    {
        if (sender is FlyoutBase flyout)
        {
            flyout.Closed -= this.OnFlyoutClosed;
            if (ReferenceEquals(this._flyout, flyout))
            {
                this._flyout = null;
            }
        }
    }
}
