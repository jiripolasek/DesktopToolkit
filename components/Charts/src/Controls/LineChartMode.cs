// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Specifies how line series are displayed.</summary>
public enum LineChartMode
{
    /// <summary>Displays each series at its original values.</summary>
    Line,

    /// <summary>Displays cumulative bands on matching X coordinates.</summary>
    StackedArea,
}
