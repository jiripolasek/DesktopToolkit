// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class SquarifiedTreemapLayout : ITreemapLayout
{
    public IReadOnlyList<TreemapPlacement> Arrange(IReadOnlyList<HierarchyChartNode> nodes, Rect bounds, int depth)
    {
        var area = bounds.Width * bounds.Height;
        if (!double.IsFinite(area) || area <= 0)
        {
            return [];
        }

        var ordered = Enumerable.Range(0, nodes.Count).Where(index => nodes[index].Value > 0).OrderByDescending(index => nodes[index].Value)
            .ToArray();
        if (ordered.Length == 0)
        {
            return [];
        }

        var largest = nodes[ordered[0]].Value;
        var scaledTotal = ordered.Sum(index => nodes[index].Value / largest);
        var areas = ordered.Select(index => ((nodes[index].Value / largest) / scaledTotal) * area).ToArray();
        var count = areas.Length;
        while (count > 0 && areas[count - 1] == 0)
        {
            count--;
        }

        var result = new List<TreemapPlacement>(count);
        var free = bounds;
        var rowStart = 0;
        while (rowStart < count && free.Width > 0 && free.Height > 0)
        {
            var side = Math.Min(free.Width, free.Height);
            var rowEnd = rowStart + 1;
            var rowArea = areas[rowStart];
            var largestInRow = rowArea;
            var worst = Worst(rowArea, largestInRow, largestInRow, side);
            while (rowEnd < count)
            {
                var candidate = areas[rowEnd];
                var nextWorst = Worst(rowArea + candidate, largestInRow, candidate, side);
                if (nextWorst > worst)
                {
                    break;
                }

                rowArea += candidate;
                worst = nextWorst;
                rowEnd++;
            }

            free = EmitRow(free, rowStart, rowEnd, rowArea);
            rowStart = rowEnd;
        }

        return result;

        Rect EmitRow(Rect frame, int from, int to, double rowArea)
        {
            // Each row follows the shorter side of the remaining space.
            var across = frame.Width <= frame.Height;
            var side = across ? frame.Width : frame.Height;
            var span = across ? frame.Height : frame.Width;
            var band = to == count ? span : Math.Min(span, rowArea / side);
            var offset = 0d;
            for (var index = from; index < to; index++)
            {
                var end = index == to - 1 ? side : Math.Min(side, offset + side * (areas[index] / rowArea));
                var tile = across
                    ? new Rect(frame.X + offset, frame.Y, end - offset, band)
                    : new Rect(frame.X, frame.Y + offset, band, end - offset);
                result.Add(new TreemapPlacement(ordered[index], tile));
                offset = end;
            }

            return across
                ? new Rect(frame.X, frame.Y + band, frame.Width, frame.Height - band)
                : new Rect(frame.X + band, frame.Y, frame.Width - band, frame.Height);
        }
    }

    private static double Worst(double sum, double largest, double smallest, double side)
    {
        var strip = sum / side;
        return strip > 0 ? Math.Max((largest / strip) / strip, strip / (smallest / strip)) : double.PositiveInfinity;
    }
}
