// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.UI;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Defines a colored threshold interval in gauge value units.</summary>
public sealed class RadialGaugeBand
{
    /// <summary>Gets the inclusive lower bound.</summary>
    public double Minimum { get; }

    /// <summary>Gets the upper bound, included when it equals the gauge maximum.</summary>
    public double Maximum { get; }

    /// <summary>Gets the band color.</summary>
    public Color Color { get; }

    /// <summary>Gets the optional status text shown when the value is within the band.</summary>
    public string? Label { get; }

    /// <summary>Creates a band with finite, increasing bounds and an optional status label.</summary>
    public RadialGaugeBand(double minimum, double maximum, Color color, string? label = null)
    {
        if (!double.IsFinite(minimum))
        {
            throw new ArgumentOutOfRangeException(nameof(minimum));
        }

        if (!double.IsFinite(maximum) || maximum <= minimum)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum), "The upper bound must be greater than the lower bound.");
        }

        this.Minimum = minimum;
        this.Maximum = maximum;
        this.Color = color;
        this.Label = label;
    }
}
