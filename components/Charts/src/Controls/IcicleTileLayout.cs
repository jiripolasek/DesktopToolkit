// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal static class IcicleTileLayout
{
    public static List<HierarchyTile> Create(HierarchyChartNode root, Rect bounds, int maxDepth, Orientation orientation)
    {
        var partitions = HierarchyPartitionLayout.Create(root, maxDepth);
        if (partitions.Count == 0)
        {
            return [];
        }

        var across = orientation == Orientation.Horizontal;
        var valueSpan = across ? bounds.Height : bounds.Width;
        var levelSpan = (across ? bounds.Width : bounds.Height) / (partitions.Max(item => item.Depth) + 1);
        var tiles = new List<HierarchyTile>(partitions.Count);
        foreach (var item in partitions)
        {
            var start = item.Start * valueSpan;
            var extent = item.End * valueSpan - start;
            var depthStart = item.Depth * levelSpan;
            var inset = Math.Min(1, Math.Min(extent, levelSpan) / 8);
            var tile = across
                ? new Rect(bounds.X + depthStart + inset, bounds.Y + start + inset, levelSpan - 2 * inset, extent - 2 * inset)
                : new Rect(bounds.X + start + inset, bounds.Y + depthStart + inset, extent - 2 * inset, levelSpan - 2 * inset);
            tiles.Add(new HierarchyTile(item.Node, tile, item.Parent, item.Branch, item.Depth));
        }

        return tiles;
    }
}
