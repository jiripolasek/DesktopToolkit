// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Controls how the live plot transitions between incoming values.</summary>
public enum LiveGraphInterpolation
{
    /// <summary>Starts and ends each transition with zero slope.</summary>
    Eased,

    /// <summary>Carries the current slope into each transition where it can do so without overshoot.</summary>
    Smooth,
}
