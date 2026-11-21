// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>A numeric or timestamped point. A null Y value marks a gap.</summary>
public readonly record struct LineChartPoint
{
    /// <summary>Gets the numeric coordinate, or null for a timestamped point.</summary>
    public double? X { get; }

    /// <summary>Gets the timestamp, or null for a numeric point.</summary>
    public DateTimeOffset? Timestamp { get; }

    /// <summary>Gets the Y value, or null to break the series at this point.</summary>
    public double? Y { get; }

    /// <summary>Creates a point on a numeric X axis.</summary>
    public LineChartPoint(double x, double? y)
    {
        if (!double.IsFinite(x))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "X must be finite.");
        }

        this.X = x;
        this.Y = ValidateY(y);
    }

    /// <summary>Creates a point on a time axis.</summary>
    public LineChartPoint(DateTimeOffset timestamp, double? y)
    {
        this.Timestamp = timestamp;
        this.Y = ValidateY(y);
    }

    private static double? ValidateY(double? y)
    {
        if (y is double value && !double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(y), "Y must be finite or null.");
        }

        return y;
    }
}
