// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Marks an event at an X coordinate or a specific point in a plot.</summary>
public sealed class ChartMilestone
{
    /// <summary>Gets the coordinate on a numeric X axis.</summary>
    public double? X { get; }

    /// <summary>Gets the UTC coordinate on a time axis.</summary>
    public DateTimeOffset? Timestamp { get; }

    /// <summary>Gets the Y coordinate. Null places the marker along the top of the plot.</summary>
    public double? Y { get; }

    /// <summary>Gets the event label.</summary>
    public string Label { get; }

    /// <summary>Gets the marker brush. Null uses the chart foreground.</summary>
    public Brush? Brush { get; init; }

    /// <summary>Creates a milestone on a numeric X axis.</summary>
    public ChartMilestone(double x, string label, double? y = null)
    {
        if (!double.IsFinite(x))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "X must be finite.");
        }

        this.X = x;
        this.Label = ValidateLabel(label);
        this.Y = ValidateY(y);
    }

    /// <summary>Creates a milestone on the time axis of a line chart.</summary>
    public ChartMilestone(DateTimeOffset timestamp, string label, double? y = null)
    {
        this.Timestamp = timestamp.ToUniversalTime();
        this.Label = ValidateLabel(label);
        this.Y = ValidateY(y);
    }

    internal double? GetX(long? timeOriginTicks)
    {
        return timeOriginTicks is { } origin ? (this.Timestamp?.UtcTicks - origin) / (double)TimeSpan.TicksPerSecond : this.X;
    }

    private static string ValidateLabel(string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        return label;
    }

    private static double? ValidateY(double? y)
    {
        return y is double value && !double.IsFinite(value) ? throw new ArgumentOutOfRangeException(nameof(y), "Y must be finite.") : y;
    }
}
