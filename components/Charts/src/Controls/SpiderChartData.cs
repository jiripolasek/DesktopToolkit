// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.UI;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>A labeled spoke with its own measurement range.</summary>
public sealed class SpiderChartAxis
{
    /// <summary>Gets the dimension label.</summary>
    public string Name { get; }

    /// <summary>Gets the value at the center.</summary>
    public double Minimum { get; }

    /// <summary>Gets the value at the outside edge.</summary>
    public double Maximum { get; }

    /// <summary>Gets the optional numeric format for values on this spoke.</summary>
    public string? ValueFormat { get; init; }

    /// <summary>Creates an axis with finite, increasing bounds.</summary>
    public SpiderChartAxis(string name, double minimum = 0, double maximum = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!double.IsFinite(minimum) || !double.IsFinite(maximum) || minimum >= maximum)
        {
            throw new ArgumentException("Axis bounds must be finite and increasing.");
        }

        this.Name = name;
        this.Minimum = minimum;
        this.Maximum = maximum;
    }
}

/// <summary>Measurements for one polygon in axis order.</summary>
public sealed class SpiderChartSeries
{
    /// <summary>Gets the series label.</summary>
    public string Name { get; }

    /// <summary>Gets the measurements in spoke order.</summary>
    public IReadOnlyList<double?> Values { get; }

    /// <summary>Gets the optional outline color.</summary>
    public Color? LineColor { get; }

    /// <summary>Gets the optional area color.</summary>
    public Color? FillColor { get; }

    /// <summary>Copies finite measurements. Null indicates a missing value.</summary>
    public SpiderChartSeries(string name, double?[] values, Color? lineColor = null, Color? fillColor = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(values);
        var snapshot = (double?[])values.Clone();
        if (Array.Exists(snapshot, static value => value.HasValue && !double.IsFinite(value.Value)))
        {
            throw new ArgumentOutOfRangeException(nameof(values), "Measurements must be finite or null.");
        }

        this.Name = name;
        this.Values = Array.AsReadOnly(snapshot);
        this.LineColor = lineColor;
        this.FillColor = fillColor;
    }
}

/// <summary>A snapshot of the dimensions and series shown by a SpiderChart.</summary>
public sealed class SpiderChartData
{
    /// <summary>Gets the axes clockwise from the top.</summary>
    public IReadOnlyList<SpiderChartAxis> Axes { get; }

    /// <summary>Gets the series in drawing order.</summary>
    public IReadOnlyList<SpiderChartSeries> Series { get; }

    /// <summary>Copies at least three axes and series with one measurement per axis.</summary>
    public SpiderChartData(SpiderChartAxis[] axes, SpiderChartSeries[] series)
    {
        ArgumentNullException.ThrowIfNull(axes);
        ArgumentNullException.ThrowIfNull(series);
        var axisSnapshot = (SpiderChartAxis[])axes.Clone();
        var seriesSnapshot = (SpiderChartSeries[])series.Clone();
        if (axisSnapshot.Length < 3)
        {
            throw new ArgumentException("Supply at least three axes.", nameof(axes));
        }

        ChartValueSnapshot.ValidateNames(axisSnapshot, static axis => axis.Name);
        ChartValueSnapshot.ValidateNames(seriesSnapshot, static item => item.Name);
        if (Array.Exists(seriesSnapshot, item => item.Values.Count != axisSnapshot.Length))
        {
            throw new ArgumentException("Supply one measurement per axis in each series.", nameof(series));
        }

        this.Axes = Array.AsReadOnly(axisSnapshot);
        this.Series = Array.AsReadOnly(seriesSnapshot);
    }
}
