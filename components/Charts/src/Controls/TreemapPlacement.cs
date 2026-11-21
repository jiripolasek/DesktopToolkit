// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Places a node from the current treemap level.</summary>
/// <param name="NodeIndex">The zero-based index of the node in the layout input.</param>
/// <param name="Bounds">The tile bounds in plot coordinates, before spacing.</param>
public readonly record struct TreemapPlacement(int NodeIndex, Rect Bounds);
