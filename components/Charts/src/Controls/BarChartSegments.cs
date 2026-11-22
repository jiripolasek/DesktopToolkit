// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

internal readonly record struct BarChartSegment(int Category, int Series, double Start, double End, double Value, double? Share);

internal sealed class BarChartSegments(IReadOnlyList<BarChartSegment> items, double minimum, double maximum)
{
    public IReadOnlyList<BarChartSegment> Items { get; } = items;

    public double Minimum { get; } = minimum;

    public double Maximum { get; } = maximum;

    public static BarChartSegments Create(BarChartData data, BarChartMode mode)
    {
        var segments = new List<BarChartSegment>();
        var minimum = 0d;
        var maximum = 0d;
        var percent = mode == BarChartMode.Stacked100;
        var stacked = percent || mode == BarChartMode.Stacked;
        for (var category = 0; category < data.Categories.Count; category++)
        {
            var positiveEnd = 0d;
            var negativeEnd = 0d;
            var (largest, scaledTotal) = percent ? GetPositiveTotal(data.Series, category) : default;
            for (var series = 0; series < data.Series.Count; series++)
            {
                if (data.Series[series].Values[category] is not double value || (stacked && value == 0) || (percent && value < 0))
                {
                    continue;
                }

                var start = 0d;
                var end = value;
                double? share = null;
                if (stacked)
                {
                    start = value > 0 ? positiveEnd : negativeEnd;
                    share = percent ? (value / largest) / scaledTotal : null;
                    end = Math.Clamp(start + (share ?? value), -double.MaxValue, percent ? 1 : double.MaxValue);
                    if (value > 0)
                    {
                        positiveEnd = end;
                    }
                    else
                    {
                        negativeEnd = end;
                    }
                }

                minimum = Math.Min(minimum, end);
                maximum = Math.Max(maximum, end);
                segments.Add(new BarChartSegment(category, series, start, end, value, share));
            }
        }

        return new BarChartSegments(segments, minimum, maximum);
    }

    private static (double Largest, double ScaledTotal) GetPositiveTotal(IReadOnlyList<BarChartSeries> series, int category)
    {
        var largest = 0d;
        foreach (var item in series)
        {
            largest = Math.Max(largest, item.Values[category].GetValueOrDefault());
        }

        if (largest == 0)
        {
            return default;
        }

        // Scale before summing so large finite values can still be normalized.
        var scaledTotal = 0d;
        foreach (var item in series)
        {
            scaledTotal += Math.Max(0, item.Values[category].GetValueOrDefault()) / largest;
        }

        return (largest, scaledTotal);
    }
}
