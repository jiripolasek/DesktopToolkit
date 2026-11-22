// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal readonly record struct SunburstTile(HierarchyPartition Partition, SunburstSector Sector);

internal static class SunburstLayout
{
    public static List<SunburstTile> Create(HierarchyChartNode root, Rect bounds, int maxDepth, double startAngle)
    {
        var radius = Math.Min(bounds.Width, bounds.Height) / 2 - 2;
        if (radius <= 0)
        {
            return [];
        }

        var partitions = HierarchyPartitionLayout.Create(root, maxDepth);
        if (partitions.Count == 0)
        {
            return [];
        }

        var depth = partitions.Max(item => item.Depth);
        var center = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        var centerRadius = depth == 0 ? radius : radius * Math.Clamp(1d / (depth + 1), 0.23, 0.33);
        var bandWidth = depth == 0 ? 0 : (radius - centerRadius) / depth;
        var radialInset = Math.Min(0.85, bandWidth / 8);
        var angle = startAngle * (Math.PI / 180);
        var tiles = new List<SunburstTile>(partitions.Count);
        foreach (var partition in partitions)
        {
            if (partition.Depth == 0)
            {
                tiles.Add(new SunburstTile(partition, new SunburstSector(center, 0, centerRadius, angle, Math.Tau, 0)));
                continue;
            }

            var inner = centerRadius + (partition.Depth - 1) * bandWidth + radialInset;
            var outer = centerRadius + partition.Depth * bandWidth - radialInset;
            var span = (partition.End - partition.Start) * Math.Tau;
            var sideInset = span == Math.Tau ? 0 : Math.Min(0.5, inner * Math.Sin(span / 8));
            var start = angle + partition.Start * Math.Tau;
            tiles.Add(new SunburstTile(partition, new SunburstSector(center, inner, outer, start, span, sideInset)));
        }

        return tiles;
    }
}
