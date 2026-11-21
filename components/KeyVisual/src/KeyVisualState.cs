// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Specifies a keycap's validation state.</summary>
public enum KeyVisualState
{
    /// <summary>Uses the style's default colors.</summary>
    Normal,

    /// <summary>Highlights an invalid key.</summary>
    Error,

    /// <summary>Highlights a key that needs attention.</summary>
    Warning,
}
