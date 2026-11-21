// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Specifies how the series in each category are displayed.</summary>
public enum BarChartMode
{
    /// <summary>Places each series beside the others.</summary>
    Grouped,

    /// <summary>Stacks positive and negative values separately from zero.</summary>
    Stacked,

    /// <summary>Stacks positive values as shares of their category total.</summary>
    Stacked100,
}
