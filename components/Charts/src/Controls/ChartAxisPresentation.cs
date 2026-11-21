// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Controls how much axis chrome a chart displays.</summary>
public enum ChartAxisPresentation
{
    /// <summary>Uses the chart and per-axis settings.</summary>
    Standard,

    /// <summary>Shows configured axis labels without axis or grid lines.</summary>
    Subtle,

    /// <summary>Hides axis labels and lines, and gives their space to the plot.</summary>
    Hidden,
}
