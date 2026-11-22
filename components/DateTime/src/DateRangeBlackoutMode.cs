// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Determines how a date range treats blackout dates between its endpoints.</summary>
public enum DateRangeBlackoutMode
{
    /// <summary>Rejects ranges containing a blackout date.</summary>
    RejectRange,

    /// <summary>Accepts the range and counts every day in it.</summary>
    Ignore,

    /// <summary>Accepts the range and omits blackout dates from the day count.</summary>
    ExcludeFromCount,
}
