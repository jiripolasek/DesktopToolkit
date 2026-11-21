// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

internal readonly record struct HierarchyPartition(HierarchyChartNode Node, double Start, double End, int Parent, int Branch, int Depth);

internal static class HierarchyPartitionLayout
{
    public static List<HierarchyPartition> Create(HierarchyChartNode root, int maxDepth)
    {
        var items = new List<HierarchyPartition>();
        if (root.Value > 0)
        {
            Append(root, 0, 1, parent: -1, branch: -1, depth: 0);
        }

        return items;

        void Append(HierarchyChartNode node, double start, double end, int parent, int branch, int depth)
        {
            var index = items.Count;
            items.Add(new HierarchyPartition(node, start, end, parent, branch, depth));
            if (depth >= maxDepth)
            {
                return;
            }

            var children = node.Children;
            var last = children.Count - 1;
            while (last >= 0 && children[last].Value == 0)
            {
                last--;
            }

            var offset = start;
            var total = 0d;
            for (var childIndex = 0; childIndex <= last; childIndex++)
            {
                var child = children[childIndex];
                if (child.Value == 0)
                {
                    continue;
                }

                total += child.Value;
                var next = childIndex == last ? end : Math.Clamp(start + (end - start) * (total / node.Value), offset, end);
                if (next > offset)
                {
                    Append(child, offset, next, index, depth == 0 ? childIndex : branch, depth + 1);
                }

                offset = next;
            }
        }
    }
}
