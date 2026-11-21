// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>A snapshot of one or more scatter series.</summary>
public sealed class ScatterChartData
{
    /// <summary>Gets the series in drawing and legend order.</summary>
    public IReadOnlyList<ScatterChartSeries> Series { get; }

    /// <summary>Creates a snapshot in the supplied drawing and legend order.</summary>
    public ScatterChartData(ScatterChartSeries[] series)
    {
        ArgumentNullException.ThrowIfNull(series);
        var snapshot = (ScatterChartSeries[])series.Clone();
        if (Array.Exists(snapshot, item => item is null))
        {
            throw new ArgumentException("Series cannot be null.", nameof(series));
        }

        this.Series = Array.AsReadOnly(snapshot);
    }
}
