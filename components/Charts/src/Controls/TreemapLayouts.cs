// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Provides reusable, stateless treemap layouts.</summary>
public static class TreemapLayouts
{
    /// <summary>Gets the default layout, which sorts by weight and favors compact tiles.</summary>
    public static ITreemapLayout Squarified { get; } = new SquarifiedTreemapLayout();

    /// <summary>Gets a layout that preserves input order and alternates the split axis at each level.</summary>
    public static ITreemapLayout SliceAndDice { get; } = new SliceAndDiceLayout();

    /// <summary>Gets a layout that divides siblings into weight-balanced groups along the longer axis.</summary>
    public static ITreemapLayout Binary { get; } = new BinaryLayout();

    private static (int Index, double Weight)[] GetWeights(IReadOnlyList<HierarchyChartNode> nodes)
    {
        var largest = nodes.Count > 0 ? nodes.Max(node => node.Value) : 0;
        return largest > 0
            ? nodes.Select((node, index) => (Index: index, Weight: node.Value / largest)).Where(item => item.Weight > 0).ToArray()
            : [];
    }

    private sealed class SliceAndDiceLayout : ITreemapLayout
    {
        public IReadOnlyList<TreemapPlacement> Arrange(IReadOnlyList<HierarchyChartNode> nodes, Rect bounds, int depth)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return [];
            }

            var weights = GetWeights(nodes);
            var result = new List<TreemapPlacement>(weights.Length);
            var total = weights.Sum(item => item.Weight);
            var across = depth % 2 == 1;
            var span = across ? bounds.Width : bounds.Height;
            var offset = 0d;
            var cumulative = 0d;
            for (var i = 0; i < weights.Length; i++)
            {
                var (index, weight) = weights[i];
                cumulative += weight;
                var end = i == weights.Length - 1 ? span : Math.Clamp(span * (cumulative / total), offset, span);
                if (end > offset)
                {
                    var tile = across
                        ? new Rect(bounds.X + offset, bounds.Y, end - offset, bounds.Height)
                        : new Rect(bounds.X, bounds.Y + offset, bounds.Width, end - offset);
                    result.Add(new TreemapPlacement(index, tile));
                }

                offset = end;
            }

            return result;
        }
    }

    private sealed class BinaryLayout : ITreemapLayout
    {
        public IReadOnlyList<TreemapPlacement> Arrange(IReadOnlyList<HierarchyChartNode> nodes, Rect bounds, int depth)
        {
            var weights = GetWeights(nodes);
            var result = new List<TreemapPlacement>(weights.Length);
            Partition(0, weights.Length, bounds);
            return result;

            void Partition(int start, int end, Rect frame)
            {
                if (start == end || frame.Width <= 0 || frame.Height <= 0)
                {
                    return;
                }

                if (end - start == 1)
                {
                    result.Add(new TreemapPlacement(weights[start].Index, frame));
                    return;
                }

                var split = start + 1;
                var tail = end - 1;
                var firstWeight = weights[start].Weight;
                var secondWeight = weights[tail].Weight;
                while (split < tail)
                {
                    if (firstWeight <= secondWeight)
                    {
                        firstWeight += weights[split++].Weight;
                    }
                    else
                    {
                        secondWeight += weights[--tail].Weight;
                    }
                }

                var fraction = firstWeight / (firstWeight + secondWeight);
                if (frame.Width >= frame.Height)
                {
                    var width = frame.Width * fraction;
                    Partition(start, split, new Rect(frame.X, frame.Y, width, frame.Height));
                    Partition(split, end, new Rect(frame.X + width, frame.Y, frame.Width - width, frame.Height));
                }
                else
                {
                    var height = frame.Height * fraction;
                    Partition(start, split, new Rect(frame.X, frame.Y, frame.Width, height));
                    Partition(split, end, new Rect(frame.X, frame.Y + height, frame.Width, frame.Height - height));
                }
            }
        }
    }
}
