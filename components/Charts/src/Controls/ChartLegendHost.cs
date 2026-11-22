// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class ChartLegendHost
{
    private readonly Control _chart;
    private readonly Grid _host;
    private readonly ChartLegendPresenter _presenter;
    private readonly ScrollViewer _scroll;

    public ChartLegendHost(Control chart, Grid host)
    {
        this._chart = chart;
        this._host = host;
        this._presenter = new ChartLegendPresenter(chart, ChartLegendSource.For(chart));
        this._scroll = this._presenter.CreateScrollViewer();
        host.Children.Clear();
        host.Children.Add(this._scroll);
    }

    public void Update(bool visible, bool limitHeight = true)
    {
        var placement = ChartLegend.GetPlacement(this._chart);
        var beside = ChartLegend.IsSide(placement);
        var hasEntries = this._presenter.Update(visible, beside ? Orientation.Vertical : Orientation.Horizontal);
        this._host.Visibility = hasEntries ? Visibility.Visible : Visibility.Collapsed;
        this._scroll.MaxHeight = beside || !limitHeight ? double.PositiveInfinity : Math.Clamp(this._chart.ActualHeight * 0.4, 24, 160);
        ChartLegend.ApplyPlacement(this._chart, hasEntries ? placement : ChartLegendPlacement.Auto);
    }
}
