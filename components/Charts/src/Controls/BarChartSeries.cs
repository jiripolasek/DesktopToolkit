// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Defines a named series with one value per category. Null values leave a gap.</summary>
public sealed class BarChartSeries
{
    /// <summary>Gets the label used in the legend and when inspecting a bar.</summary>
    public string Name { get; }

    /// <summary>Gets or sets an optional unique key that identifies this series across data snapshots.</summary>
    public string? Key { get; init; }

    /// <summary>Gets values in category order.</summary>
    public IReadOnlyList<double?> Values { get; }

    /// <summary>Gets the series brush, or null to use the default palette.</summary>
    public Brush? Brush { get; }

    /// <summary>Creates a series and takes a snapshot of its values.</summary>
    public BarChartSeries(string name, double?[] values, Brush? brush = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(values);

        var snapshot = (double?[])values.Clone();
        if (Array.Exists(snapshot, value => value.HasValue && !double.IsFinite(value.Value)))
        {
            throw new ArgumentException("Series values must be finite or null.", nameof(values));
        }

        this.Name = name;
        this.Values = Array.AsReadOnly(snapshot);
        this.Brush = brush;
    }
}
