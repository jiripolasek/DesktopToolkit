// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Arranges one level of a treemap at a time.</summary>
public interface ITreemapLayout
{
    /// <summary>Positions siblings in the space available for their tiles.</summary>
    /// <param name="nodes">The siblings in data order, including any zero-value nodes.</param>
    /// <param name="bounds">The available rectangle in plot coordinates, before tile spacing.</param>
    /// <param name="depth">The visible level, starting at one.</param>
    /// <returns>The placements in drawing and inspection order.</returns>
    /// <remarks>
    /// Return at most one placement per input index. Use finite, non-overlapping bounds
    /// inside the available rectangle. Area should be proportional to each node's value.
    /// The chart applies spacing, expands branches, and reruns the layout on the UI thread as needed.
    /// </remarks>
    IReadOnlyList<TreemapPlacement> Arrange(IReadOnlyList<HierarchyChartNode> nodes, Rect bounds, int depth);
}
