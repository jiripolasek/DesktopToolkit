// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Defines both vertical bounds of a live graph.</summary>
public sealed class LiveGraphRange
{
    /// <summary>Gets the value at the bottom of the plot.</summary>
    public double Minimum { get; }

    /// <summary>Gets the value at the top of the plot.</summary>
    public double Maximum { get; }

    /// <summary>Creates a range with finite, increasing bounds.</summary>
    public LiveGraphRange(double minimum, double maximum)
    {
        if (!double.IsFinite(minimum) || !double.IsFinite(maximum) || minimum >= maximum)
        {
            throw new ArgumentException("Graph bounds must be finite and increasing.");
        }

        this.Minimum = minimum;
        this.Maximum = maximum;
    }
}
