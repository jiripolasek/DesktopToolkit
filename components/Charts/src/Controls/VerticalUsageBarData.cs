// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>A snapshot of stacked contributions for a usage meter.</summary>
public sealed class VerticalUsageBarData
{
    private readonly double[] _values;

    /// <summary>Gets the stacked series in display order.</summary>
    public IReadOnlyList<VerticalUsageBarSeries> Series { get; }

    /// <summary>Gets the contributions in series order.</summary>
    public IReadOnlyList<double> Values { get; }

    internal double[] ValueArray => this._values;

    /// <summary>Copies the series and their nonnegative contributions in display order.</summary>
    public VerticalUsageBarData(VerticalUsageBarSeries[] series, double[] values)
    {
        ArgumentNullException.ThrowIfNull(series);
        var items = (VerticalUsageBarSeries[])series.Clone();
        this._values = ChartValueSnapshot.Copy(values, items.Length);
        ChartValueSnapshot.ValidateNames(items, static item => item.Name);
        if (Array.Exists(items, item => item.IndicatorBrush is null))
        {
            throw new ArgumentException("Every series needs a brush.", nameof(series));
        }

        this.Series = Array.AsReadOnly(items);
        this.Values = Array.AsReadOnly(this._values);
    }
}
