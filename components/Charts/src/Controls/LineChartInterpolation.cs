// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Specifies how a line chart connects adjacent samples.</summary>
public enum LineChartInterpolation
{
    /// <summary>Connects samples with straight segments.</summary>
    Linear,

    /// <summary>Uses shape-preserving cubic curves between adjacent samples.</summary>
    Monotone,
}
