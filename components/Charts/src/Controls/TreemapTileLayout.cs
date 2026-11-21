// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal static class TreemapTileLayout
{
    public const double HeaderSpacing = 8;
    private const double EdgeTolerance = 0.001;

    public static List<HierarchyTile> Create(
        HierarchyChartNode root,
        Rect bounds,
        int maxDepth,
        double headerHeight,
        double tileSpacing,
        double branchPadding,
        ITreemapLayout layout)
    {
        var tiles = new List<HierarchyTile>();
        IReadOnlyList<HierarchyChartNode> nodes = root.Children.Count == 0 ? (HierarchyChartNode[])[root] : root.Children;
        var branchColors = new int[nodes.Count];
        var color = 0;
        foreach (var index in Enumerable.Range(0, nodes.Count).OrderByDescending(index => nodes[index].Value))
        {
            branchColors[index] = color++;
        }

        var halfGap = tileSpacing / 2;

        Append(nodes, bounds, parent: -1, branch: 0, depth: 1);
        return tiles;

        void Append(IReadOnlyList<HierarchyChartNode> siblings, Rect frame, int parent, int branch, int depth)
        {
            var seen = new HashSet<int>();
            foreach (var (index, area) in layout.Arrange(siblings, frame, depth))
            {
                if ((uint)index >= (uint)siblings.Count
                    || siblings[index].Value <= 0
                    || !TryGetTile(area, frame, halfGap, insetOuterEdges: parent < 0, out var tile)
                    || !seen.Add(index))
                {
                    continue;
                }

                var node = siblings[index];
                var colorBranch = parent < 0 ? branchColors[index] : branch;
                var tileIndex = tiles.Count;
                var contentWidth = tile.Width - 2 * branchPadding;
                var contentHeight = tile.Height - headerHeight - branchPadding;
                var expands = depth < maxDepth
                              && contentWidth >= 32
                              && contentHeight >= 6
                              && tile.Width >= 48
                              && node.Children.Any(child => child.Value > 0);
                tiles.Add(new HierarchyTile(node, tile, parent, colorBranch, depth, expands));
                if (expands)
                {
                    var content = new Rect(tile.X + branchPadding, tile.Y + headerHeight, contentWidth, contentHeight);
                    Append(node.Children, content, tileIndex, colorBranch, depth + 1);
                }
            }
        }
    }

    private static bool TryGetTile(Rect area, Rect frame, double halfGap, bool insetOuterEdges, out Rect tile)
    {
        tile = default;
        if (!double.IsFinite(area.Left) || !double.IsFinite(area.Top) || !double.IsFinite(area.Right) || !double.IsFinite(area.Bottom))
        {
            return false;
        }

        var left = Math.Max(area.Left, frame.Left);
        var top = Math.Max(area.Top, frame.Top);
        var right = Math.Min(area.Right, frame.Right);
        var bottom = Math.Min(area.Bottom, frame.Bottom);
        var width = right - left;
        var height = bottom - top;
        if (width < 1 || height < 1)
        {
            return false;
        }

        var inset = Math.Min(halfGap, Math.Min(width, height) / 8);
        // Branch padding already accounts for the edges of a child frame.
        left += insetOuterEdges || left - frame.Left > EdgeTolerance ? inset : 0;
        top += insetOuterEdges || top - frame.Top > EdgeTolerance ? inset : 0;
        right -= insetOuterEdges || frame.Right - right > EdgeTolerance ? inset : 0;
        bottom -= insetOuterEdges || frame.Bottom - bottom > EdgeTolerance ? inset : 0;
        tile = new Rect(left, top, right - left, bottom - top);
        return tile.Width >= 1 && tile.Height >= 1;
    }
}
