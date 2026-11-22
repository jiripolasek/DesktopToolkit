// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal readonly record struct MonotoneSegment(Point Control1, Point Control2, Point End, bool IsVertical = false);

internal static class MonotoneCurve
{
    public static MonotoneSegment[] CreateSegments(IReadOnlyList<Point> points)
    {
        var secants = new double[points.Count - 1];
        for (var index = 0; index < secants.Length; index++)
        {
            var start = points[index];
            var end = points[index + 1];
            var width = end.X - start.X;
            secants[index] = width > 0 ? (end.Y - start.Y) / width : 0;
        }

        var tangents = new double[points.Count];
        tangents[0] = secants[0];
        tangents[^1] = secants[^1];
        for (var index = 1; index < tangents.Length - 1; index++)
        {
            var leftSlope = secants[index - 1];
            var rightSlope = secants[index];
            if (leftSlope == 0 || rightSlope == 0 || Math.Sign(leftSlope) != Math.Sign(rightSlope))
            {
                continue;
            }

            // Fritsch-Butland weighted harmonic mean for unequal intervals.
            var leftWidth = points[index].X - points[index - 1].X;
            var rightWidth = points[index + 1].X - points[index].X;
            var leftWeight = leftWidth + 2 * rightWidth;
            var rightWeight = 2 * leftWidth + rightWidth;
            tangents[index] = (leftWeight + rightWeight) / (leftWeight / leftSlope + rightWeight / rightSlope);
        }

        var segments = new MonotoneSegment[secants.Length];
        for (var index = 0; index < segments.Length; index++)
        {
            var start = points[index];
            var end = points[index + 1];
            var thirdWidth = (end.X - start.X) / 3;
            if (thirdWidth <= 0)
            {
                segments[index] = new MonotoneSegment(default, default, end, IsVertical: true);
                continue;
            }

            var minimum = Math.Min(start.Y, end.Y);
            var maximum = Math.Max(start.Y, end.Y);
            segments[index] = new MonotoneSegment(
                new Point(start.X + thirdWidth, Math.Clamp(start.Y + tangents[index] * thirdWidth, minimum, maximum)),
                new Point(end.X - thirdWidth, Math.Clamp(end.Y - tangents[index + 1] * thirdWidth, minimum, maximum)), end);
        }

        return segments;
    }
}
