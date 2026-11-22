// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Specifies which bars share an inspection tooltip.</summary>
public enum BarChartInspectionMode
{
    /// <summary>Shows one bar or stack segment.</summary>
    Bar,

    /// <summary>Shows all plotted values for the selected category.</summary>
    Category,

    /// <summary>Shows the selected series across all plotted categories.</summary>
    Series,
}
