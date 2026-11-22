// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.UI;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>
/// Describes one immutable visual series in a <see cref="LiveAreaGraph"/>.
/// </summary>
public sealed class LiveGraphSeries
{
    /// <summary>Gets the unique name displayed in the legend and inspection tooltip.</summary>
    public string Name { get; }

    /// <summary>
    /// Gets the color used for the upper edge of the area.
    /// </summary>
    public Color EdgeLineColor { get; }

    /// <summary>Gets the color, including opacity, used to fill the area.</summary>
    public Color FillColor { get; }

    /// <summary>
    /// Gets the upper-edge thickness in effective pixels.
    /// </summary>
    public float EdgeLineThickness { get; }

    /// <summary>
    /// Gets whether the upper edge is hidden, solid, or dashed.
    /// </summary>
    public LiveGraphEdgeLineStyle EdgeLineStyle { get; }

    /// <summary>Creates an immutable series descriptor with a unique name within its graph.</summary>
    public LiveGraphSeries(
        string name,
        Color edgeLineColor,
        Color fillColor,
        float edgeLineThickness = 2f,
        LiveGraphEdgeLineStyle edgeLineStyle = LiveGraphEdgeLineStyle.Solid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!float.IsFinite(edgeLineThickness) || edgeLineThickness <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(edgeLineThickness));
        }

        if (!Enum.IsDefined(edgeLineStyle))
        {
            throw new ArgumentOutOfRangeException(nameof(edgeLineStyle));
        }

        this.Name = name;
        this.EdgeLineColor = edgeLineColor;
        this.FillColor = fillColor;
        this.EdgeLineThickness = edgeLineThickness;
        this.EdgeLineStyle = edgeLineStyle;
    }
}
