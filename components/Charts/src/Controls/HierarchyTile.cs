// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal readonly record struct HierarchyTile(
    HierarchyChartNode Node,
    Rect Bounds,
    int Parent,
    int Branch,
    int Depth,
    bool IsExpanded = false);
