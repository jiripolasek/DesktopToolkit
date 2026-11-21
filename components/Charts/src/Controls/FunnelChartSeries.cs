// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Measurements for one cohort across the ordered stages of a funnel.</summary>
public sealed class FunnelChartSeries
{
    /// <summary>Gets the cohort label.</summary>
    public string Label { get; }

    /// <summary>Gets the measurements in stage order.</summary>
    public IReadOnlyList<double> Values { get; }

    /// <summary>Gets the optional fill that overrides the chart palette.</summary>
    public Brush? Brush { get; }

    /// <summary>Creates a snapshot with one finite, non-negative value per stage.</summary>
    public FunnelChartSeries(string label, double[] values, Brush? brush = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(values);
        var snapshot = (double[])values.Clone();
        if (Array.Exists(snapshot, value => !double.IsFinite(value) || value < 0))
        {
            throw new ArgumentException("Each stage value must be finite and non-negative.", nameof(values));
        }

        this.Label = label;
        this.Values = Array.AsReadOnly(snapshot);
        this.Brush = brush;
    }
}
