// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.UI;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>A named set of points with an optional marker color.</summary>
public sealed class ScatterChartSeries
{
    /// <summary>Gets the series name.</summary>
    public string Name { get; }

    /// <summary>Gets the point snapshot.</summary>
    public IReadOnlyList<ScatterChartPoint> Points { get; }

    /// <summary>Gets the marker color, or null to use the chart palette.</summary>
    public Color? MarkerColor { get; }

    /// <summary>Creates a snapshot of the supplied points. Points may repeat and can appear in any order.</summary>
    public ScatterChartSeries(string name, ScatterChartPoint[] points, Color? markerColor = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(points);

        this.Name = name;
        this.Points = Array.AsReadOnly((ScatterChartPoint[])points.Clone());
        this.MarkerColor = markerColor;
    }
}
