// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class DoughnutLegendPresenter
{
    private readonly DoughnutGraph _owner;
    private readonly ScrollViewer? _view;
    private readonly ChartLegendPresenter? _legend;
    private readonly ChartLegendSource _source;
    private readonly Action<int> _highlight;
    private DoughnutGraphSeries[] _series = [];
    private SolidColorBrush[] _brushes = [];

    public DoughnutLegendPresenter(DoughnutGraph owner, ScrollViewer? view, Action<int> highlight)
    {
        this._owner = owner;
        this._view = view;
        this._source = ChartLegendSource.For(owner);
        this._highlight = highlight;
        if (view is not null)
        {
            this._legend = new ChartLegendPresenter(owner, this._source);
            view.Content = this._legend.Root;
        }
    }

    public void Update(DoughnutGraphSeries[] series, double[] shares, double[] values)
    {
        if (this._series != series)
        {
            this._series = series;
            this._brushes = series.Select(static slice => new SolidColorBrush(slice.Color)).ToArray();
        }

        var entries = new ChartLegendEntry[series.Length];
        var mode = ChartLegend.GetValueMode(this._owner);
        for (var index = 0; index < series.Length; index++)
        {
            var value = mode switch
            {
                ChartLegendValueMode.Hidden => null,
                ChartLegendValueMode.Value => ChartInspectorPresenter.FormatValue(
                    index < values.Length ? values[index] : 0, this._owner.ValueFormat),
                _ => (index < shares.Length ? shares[index] : 0).ToString("0.#%", CultureInfo.CurrentCulture),
            };
            entries[index] = new(series[index].Name, this._brushes[index], value);
        }

        this._source.Update(ChartLegendSwatch.Box, entries, this._highlight);
        this.UpdateLayout();
    }

    public void UpdateLayout()
    {
        if (this._view is null || this._legend is null)
        {
            return;
        }

        var visible = this._owner.ShowLegend && this._series.Length > 0;
        var placement = visible ? ChartLegend.GetPlacement(this._owner) : ChartLegendPlacement.Bottom;
        if (placement == ChartLegendPlacement.Auto
            && this._owner.ActualWidth >= 360
            && ChartLegend.GetLayout(this._owner) != ChartLegendLayout.Horizontal)
        {
            placement = ChartLegendPlacement.End;
        }

        var beside = ChartLegend.IsSide(placement);
        this._legend.Update(visible, beside ? Orientation.Vertical : Orientation.Horizontal);
        this._view.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        this._view.MaxHeight = beside ? double.PositiveInfinity : Math.Clamp((this._owner.ActualHeight - 24) * 0.42, 28, 112);
        ChartLegend.ApplyPlacement(this._owner, placement);
    }
}
