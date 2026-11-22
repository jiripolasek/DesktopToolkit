// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class UsageBarLegendPresenter
{
    private readonly VerticalUsageBar _owner;
    private readonly ContentControl? _host;
    private readonly ChartLegendSource _source;
    private readonly ChartLegendPresenter? _legend;
    private readonly Action<int> _highlight;

    public bool IsVisible => this._host?.Visibility == Visibility.Visible;

    public UsageBarLegendPresenter(VerticalUsageBar owner, ContentControl? host, Action<int> highlight)
    {
        this._owner = owner;
        this._host = host;
        this._source = ChartLegendSource.For(owner);
        this._highlight = highlight;
        if (host is not null)
        {
            this._legend = new ChartLegendPresenter(owner, this._source);
            host.Content = this._legend.Root;
        }
    }

    public static ChartLegendPlacement ResolvePlacement(VerticalUsageBar owner)
    {
        var placement = ChartLegend.GetPlacement(owner);
        if (placement != ChartLegendPlacement.Auto)
        {
            return placement;
        }

        var leading = owner.LegendPlacement == UsageBarLegendPlacement.Start;
        return owner.Orientation == Orientation.Horizontal ? leading ? ChartLegendPlacement.Top : ChartLegendPlacement.Bottom :
            leading ? ChartLegendPlacement.Start : ChartLegendPlacement.End;
    }

    public void Update(IReadOnlyList<VerticalUsageBarSeries> series, double[] values)
    {
        var entries = new ChartLegendEntry[series.Count];
        var showValues = ChartLegend.GetValueMode(this._owner) switch
        {
            ChartLegendValueMode.Value => true,
            ChartLegendValueMode.Hidden => false,
            _ => this._owner.ShowLegendValues,
        };
        for (var index = 0; index < series.Count; index++)
        {
            var value = showValues
                ? ChartInspectorPresenter.FormatValue(index < values.Length ? values[index] : 0, this._owner.ValueFormat)
                : null;
            entries[index] = new(series[index].Name, series[index].IndicatorBrush, value);
        }

        this._source.Update(ChartLegendSwatch.Dot, entries, this._highlight);

        if (this._host is null || this._legend is null)
        {
            return;
        }

        var placement = ResolvePlacement(this._owner);
        var beside = ChartLegend.IsSide(placement);
        var orientation = ChartLegend.GetPlacement(this._owner) == ChartLegendPlacement.Auto ? this._owner.LegendOrientation :
            beside ? Orientation.Vertical : Orientation.Horizontal;
        this._host.Visibility = this._legend.Update(this._owner.ShowLegend, orientation) ? Visibility.Visible : Visibility.Collapsed;
        Grid.SetRow(
            this._host, placement switch
            {
                ChartLegendPlacement.Top => 0,
                ChartLegendPlacement.Bottom => 2,
                _ => 1,
            });
        Grid.SetColumn(
            this._host, placement switch
            {
                ChartLegendPlacement.Start => 0,
                ChartLegendPlacement.End => 2,
                _ => 1,
            });
        this._host.Margin = placement switch
        {
            ChartLegendPlacement.Top => new Thickness(0, 0, 0, 8),
            ChartLegendPlacement.Bottom => new Thickness(0, 8, 0, 0),
            ChartLegendPlacement.Start => new Thickness(0, 0, 12, 0),
            _ => new Thickness(12, 0, 0, 0),
        };
        this._host.VerticalAlignment = beside ? VerticalAlignment.Center : VerticalAlignment.Stretch;
        this._host.MaxWidth = beside ? 200 : double.PositiveInfinity;
    }
}
