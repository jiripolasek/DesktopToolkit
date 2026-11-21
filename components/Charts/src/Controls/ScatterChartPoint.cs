// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>A scatter point with finite numeric coordinates and an optional label.</summary>
public readonly record struct ScatterChartPoint
{
    /// <summary>Gets the X coordinate.</summary>
    public double X { get; }

    /// <summary>Gets the Y coordinate.</summary>
    public double Y { get; }

    /// <summary>Gets the optional label shown when inspecting this point.</summary>
    public string? Label { get; }

    /// <summary>Creates a point on the numeric X and Y axes.</summary>
    public ScatterChartPoint(double x, double y, string? label = null)
    {
        if (!double.IsFinite(x))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "X must be finite.");
        }

        if (!double.IsFinite(y))
        {
            throw new ArgumentOutOfRangeException(nameof(y), "Y must be finite.");
        }

        this.X = x;
        this.Y = y;
        this.Label = label;
    }
}
