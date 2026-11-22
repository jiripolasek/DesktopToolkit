// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Specifies which corners can be rounded in stacked bar charts.</summary>
/// <remarks>Corners touching a visible zero baseline stay square in both modes.</remarks>
public enum BarChartSegmentRounding
{
    /// <summary>Rounds the outside corners of the stack and leaves segment joins square.</summary>
    /// <remarks>The zero join stays square when positive and negative segments meet.</remarks>
    OuterCorners,

    /// <summary>Rounds each visible segment independently.</summary>
    AllCorners,
}
