// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Describes the data targeted by a chart context request.</summary>
public sealed class ChartContextMenuRequest
{
    /// <summary>Gets the chart that received the request.</summary>
    public Control Chart { get; }

    /// <summary>Gets the menu anchor in DIPs relative to the chart.</summary>
    public Point Position { get; }

    /// <summary>Gets whether the request came from a pointer gesture.</summary>
    public bool IsPointerRequest { get; }

    /// <summary>Gets the formatted inspector readout for the targeted data.</summary>
    public string Description { get; }

    /// <summary>Gets the targeted data, captured when the context menu was requested.</summary>
    public IReadOnlyList<ChartContextValue> Values { get; }

    internal ChartContextMenuRequest(Control chart, Point position, bool isPointerRequest, string description, ChartContextValue[] values)
    {
        this.Chart = chart;
        this.Position = position;
        this.IsPointerRequest = isPointerRequest;
        this.Description = description;
        this.Values = Array.AsReadOnly(values);
    }
}

/// <summary>One data item or measurement targeted by a chart context request.</summary>
/// <param name="Label">The node, series, or measurement label.</param>
/// <param name="Value">The raw value, or null for a threshold interval.</param>
/// <param name="Item">The source node, data point, series, or band, when available.</param>
/// <param name="SeriesIndex">The index in the chart's series collection, when applicable.</param>
/// <param name="CategoryIndex">The index in a bar chart's category collection, when applicable.</param>
/// <param name="X">The numeric X coordinate, when applicable.</param>
/// <param name="Timestamp">The time coordinate, when applicable.</param>
/// <param name="Category">The category label, when applicable.</param>
public readonly record struct ChartContextValue(
    string Label,
    double? Value,
    object? Item = null,
    int? SeriesIndex = null,
    int? CategoryIndex = null,
    double? X = null,
    DateTimeOffset? Timestamp = null,
    string? Category = null);
