// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>
/// Selects the background guide rendered by <see cref="LiveAreaGraph"/>.
/// </summary>
public enum LiveGraphGridMode
{
    /// <summary>Draws no background guide.</summary>
    None,

    /// <summary>Draws horizontal and vertical guide lines.</summary>
    Cross,

    /// <summary>Draws dots at the guide intersections.</summary>
    Dot,
}
