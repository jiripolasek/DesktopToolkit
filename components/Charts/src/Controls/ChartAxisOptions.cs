// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Optional settings for a Cartesian axis.</summary>
public sealed class ChartAxisOptions
{
    private readonly double _labelOpacity = 1;
    private readonly IReadOnlyList<ChartReferenceLine> _referenceLines = [];

    /// <summary>Gets the fixed numeric lower bound.</summary>
    public double? Minimum { get; }

    /// <summary>Gets the fixed numeric upper bound.</summary>
    public double? Maximum { get; }

    /// <summary>Gets the format for ticks. Inspector formatting is independent.</summary>
    public string? TickLabelFormat { get; }

    /// <summary>Gets the grid override. Null follows the chart setting.</summary>
    public bool? ShowGridLines { get; }

    /// <summary>Gets whether to show tick labels.</summary>
    public bool ShowLabels { get; }

    /// <summary>Gets whether to draw the axis line. Defaults to true.</summary>
    public bool ShowAxisLine { get; init; } = true;

    /// <summary>Gets the reference lines on a supported numeric or time axis.</summary>
    /// <remarks>The lines are copied and do not affect the axis range.</remarks>
    public IReadOnlyList<ChartReferenceLine> ReferenceLines
    {
        get => this._referenceLines;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            var snapshot = value.ToArray();
            if (Array.IndexOf(snapshot, null) >= 0)
            {
                throw new ArgumentException("Reference lines cannot contain null entries.", nameof(value));
            }

            this._referenceLines = Array.AsReadOnly(snapshot);
        }
    }

    /// <summary>Gets the opacity of tick labels, from zero to one. Defaults to one.</summary>
    public double LabelOpacity
    {
        get => this._labelOpacity;
        init => this._labelOpacity = double.IsFinite(value) && value is >= 0 and <= 1
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value));
    }

    /// <summary>Creates axis options. Null numeric bounds are calculated from data.</summary>
    public ChartAxisOptions(
        double? minimum = null,
        double? maximum = null,
        string? tickLabelFormat = null,
        bool? showGridLines = null,
        bool showLabels = true)
    {
        if (minimum is double min && (!double.IsFinite(min) || min == double.MaxValue))
        {
            throw new ArgumentOutOfRangeException(nameof(minimum));
        }

        if (maximum is double max && (!double.IsFinite(max) || max == -double.MaxValue))
        {
            throw new ArgumentOutOfRangeException(nameof(maximum));
        }

        if (minimum >= maximum)
        {
            throw new ArgumentException("Minimum must be lower than maximum.");
        }

        this.Minimum = minimum;
        this.Maximum = maximum;
        this.TickLabelFormat = tickLabelFormat;
        this.ShowGridLines = showGridLines;
        this.ShowLabels = showLabels;
    }
}
