// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>A snapshot of slice definitions and values for a doughnut or pie.</summary>
public sealed class DoughnutGraphData
{
    private readonly double[] _values;

    /// <summary>Gets the slice definitions in display order.</summary>
    public IReadOnlyList<DoughnutGraphSeries> Series { get; }

    /// <summary>Gets the values in slice order.</summary>
    public IReadOnlyList<double> Values { get; }

    internal double[] ValueArray => this._values;

    /// <summary>Copies the slices and their nonnegative values in display order.</summary>
    public DoughnutGraphData(DoughnutGraphSeries[] series, double[] values)
    {
        ArgumentNullException.ThrowIfNull(series);
        var items = (DoughnutGraphSeries[])series.Clone();
        this._values = ChartValueSnapshot.Copy(values, items.Length);
        ChartValueSnapshot.ValidateNames(items, static item => item.Name);
        this.Series = Array.AsReadOnly(items);
        this.Values = Array.AsReadOnly(this._values);
    }
}
