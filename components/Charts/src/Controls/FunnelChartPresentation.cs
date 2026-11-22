// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Specifies how a funnel chart presents its stages.</summary>
public enum FunnelChartPresentation
{
    /// <summary>Shows separate horizontal bands ordered from top to bottom.</summary>
    Stages,

    /// <summary>Connects stage measurements as ribbons from left to right.</summary>
    Flow,
}
