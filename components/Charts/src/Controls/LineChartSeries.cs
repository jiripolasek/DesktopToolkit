// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.UI;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Defines a named series, with points ordered by X coordinate.</summary>
public sealed class LineChartSeries
{
    private double _strokeThickness = 2;

    /// <summary>Gets the label used in the legend and inspection.</summary>
    public string Name { get; }

    /// <summary>Gets the ordered points, including gaps.</summary>
    public IReadOnlyList<LineChartPoint> Points { get; }

    /// <summary>Gets the line color, or null to use the default palette.</summary>
    public Color? LineColor { get; }

    /// <summary>Gets the area color, or null to use a translucent version of the line color.</summary>
    public Color? FillColor { get; }

    /// <summary>Gets the stroke thickness in effective pixels. Defaults to 2.</summary>
    public double StrokeThickness
    {
        get => this._strokeThickness;
        init => this._strokeThickness = double.IsFinite(value) && value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    /// <summary>Gets whether to use a dashed stroke.</summary>
    public bool IsStrokeDashed { get; init; }

    /// <summary>Gets the fill override for ordinary lines. Null follows the chart setting.</summary>
    public bool? ShowAreaFill { get; init; }

    /// <summary>Takes a snapshot of the supplied points. Equal X values are allowed.</summary>
    public LineChartSeries(string name, LineChartPoint[] points, Color? lineColor = null, Color? fillColor = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(points);

        var snapshot = (LineChartPoint[])points.Clone();
        for (var index = 0; index < snapshot.Length; index++)
        {
            var point = snapshot[index];
            if (point.X is null && point.Timestamp is null)
            {
                throw new ArgumentException("Every point must have an X coordinate.", nameof(points));
            }

            if (index == 0)
            {
                continue;
            }

            var previous = snapshot[index - 1];
            if (point.Timestamp.HasValue != previous.Timestamp.HasValue)
            {
                throw new ArgumentException("A series must use one X axis type.", nameof(points));
            }

            if (point.Timestamp < previous.Timestamp || point.X < previous.X)
            {
                throw new ArgumentException("Points must be ordered by ascending X coordinate.", nameof(points));
            }
        }

        this.Name = name;
        this.Points = Array.AsReadOnly(snapshot);
        this.LineColor = lineColor;
        this.FillColor = fillColor;
    }
}
