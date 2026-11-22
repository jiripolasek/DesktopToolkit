// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>A target, limit, or other reference coordinate on a chart axis.</summary>
public sealed class ChartReferenceLine
{
    private readonly double _strokeThickness = 1;

    /// <summary>Gets the coordinate of a numeric reference line.</summary>
    public double? Value { get; }

    /// <summary>Gets the UTC coordinate of a time reference line.</summary>
    public DateTimeOffset? Timestamp { get; }

    /// <summary>Gets the optional text displayed alongside the line.</summary>
    public string? Label { get; }

    /// <summary>Gets the stroke brush. Null uses the chart foreground.</summary>
    public Brush? Stroke { get; init; }

    /// <summary>Gets whether the stroke is dashed. Defaults to true.</summary>
    public bool IsStrokeDashed { get; init; } = true;

    /// <summary>Gets the stroke thickness in DIPs. Defaults to one.</summary>
    public double StrokeThickness
    {
        get => this._strokeThickness;
        init => this._strokeThickness = double.IsFinite(value) && value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    /// <summary>Creates a reference line on a numeric axis.</summary>
    public ChartReferenceLine(double value, string? label = null)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "The reference value must be finite.");
        }

        this.Value = value;
        this.Label = label;
    }

    /// <summary>Creates a reference line on the time axis of a line chart.</summary>
    public ChartReferenceLine(DateTimeOffset timestamp, string? label = null)
    {
        this.Timestamp = timestamp.ToUniversalTime();
        this.Label = label;
    }

    internal double? ProjectValue(long? timeOriginTicks)
    {
        return timeOriginTicks is { } origin ? (this.Timestamp?.UtcTicks - origin) / (double)TimeSpan.TicksPerSecond : this.Value;
    }
}
