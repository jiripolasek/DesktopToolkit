// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

internal readonly record struct StackedAreaPoint(double X, double Baseline, double Boundary);

internal readonly record struct StackedAreaSample(LineChartPoint Source, double X, double Top, bool IsIsolated);

internal sealed class StackedAreaLayer
{
    private List<StackedAreaPoint>? _run;
    private bool _isNegative;

    public List<List<StackedAreaPoint>> Runs { get; } = [];

    public List<StackedAreaSample> Samples { get; } = [];

    public void AddInterval(StackedAreaPoint start, StackedAreaPoint end, bool isNegative)
    {
        if (this._run is null || this._isNegative != isNegative || this._run[^1] != start)
        {
            this._run = [start];
            this._isNegative = isNegative;
            this.Runs.Add(this._run);
        }

        this._run.Add(end);
    }

    public void BreakRun()
    {
        this._run = null;
    }
}

internal sealed class LineChartStack(StackedAreaLayer[] layers, double minimum, double maximum)
{
    public StackedAreaLayer[] Layers { get; } = layers;

    public double Minimum { get; } = minimum;

    public double Maximum { get; } = maximum;

    public static LineChartStack? Create(LineChartData data)
    {
        var series = data.Series;
        var reference = series[0].Points;
        foreach (var item in series)
        {
            if (item.Points.Count != reference.Count)
            {
                return null;
            }

            for (var index = 0; index < reference.Count; index++)
            {
                var point = item.Points[index];
                if (point.X != reference[index].X || point.Timestamp != reference[index].Timestamp)
                {
                    return null;
                }
            }
        }

        var layers = new StackedAreaLayer[series.Count];
        for (var index = 0; index < layers.Length; index++)
        {
            layers[index] = new StackedAreaLayer();
        }

        var positions = new double[reference.Count];
        var available = new int[reference.Count];
        for (var column = 0; column < reference.Count; column++)
        {
            positions[column] = data.GetX(reference[column]);
            while (available[column] < series.Count && series[available[column]].Points[column].Y.HasValue)
            {
                available[column]++;
            }
        }

        var minimum = 0d;
        var maximum = 0d;
        for (var column = 0; column < positions.Length; column++)
        {
            var positive = 0d;
            var negative = 0d;
            for (var index = 0; index < available[column]; index++)
            {
                var sample = series[index].Points[column];
                var top = Accumulate(sample.Y!.Value, ref positive, ref negative);
                var hasPrevious = column > 0 && available[column - 1] > index;
                var hasNext = column + 1 < positions.Length && available[column + 1] > index;
                layers[index].Samples.Add(new StackedAreaSample(sample, positions[column], top, !hasPrevious && !hasNext));
            }

            minimum = Math.Min(minimum, negative);
            maximum = Math.Max(maximum, positive);
        }

        var splits = new List<double>();
        var crossings = new double[series.Count];
        for (var column = 1; column < positions.Length; column++)
        {
            var count = Math.Min(available[column - 1], available[column]);
            for (var index = count; index < layers.Length; index++)
            {
                layers[index].BreakRun();
            }

            if (count == 0)
            {
                continue;
            }

            // Insert a shared knot wherever a contribution changes sign.
            splits.Clear();
            splits.Add(0);
            for (var index = 0; index < count; index++)
            {
                var first = series[index].Points[column - 1].Y!.Value;
                var last = series[index].Points[column].Y!.Value;
                var crossing = GetCrossing(first, last);
                crossings[index] = crossing;
                if (crossing > 0 && crossing < 1)
                {
                    splits.Add(crossing);
                }
            }

            splits.Add(1);
            splits.Sort();
            for (var split = 1; split < splits.Count; split++)
            {
                var from = splits[split - 1];
                var to = splits[split];
                if (to > from)
                {
                    AddInterval(series, layers, positions, crossings, column, count, from, to);
                }
            }
        }

        return new LineChartStack(layers, minimum, maximum);
    }

    private static void AddInterval(
        IReadOnlyList<LineChartSeries> series,
        StackedAreaLayer[] layers,
        double[] positions,
        double[] crossings,
        int column,
        int count,
        double from,
        double to)
    {
        var firstX = Interpolate(positions[column - 1], positions[column], from);
        var lastX = Interpolate(positions[column - 1], positions[column], to);
        var startPositive = 0d;
        var startNegative = 0d;
        var endPositive = 0d;
        var endNegative = 0d;
        for (var index = 0; index < count; index++)
        {
            var first = series[index].Points[column - 1].Y!.Value;
            var last = series[index].Points[column].Y!.Value;
            var crossing = crossings[index];
            var start = from == crossing ? 0 : Interpolate(first, last, from);
            var end = to == crossing ? 0 : Interpolate(first, last, to);
            var isNegative = double.IsNaN(crossing) ? first < 0 || last < 0 : from < crossing ? first < 0 : last < 0;
            start = isNegative ? Math.Min(0, start) : Math.Max(0, start);
            end = isNegative ? Math.Min(0, end) : Math.Max(0, end);
            if (start == 0 && end == 0)
            {
                layers[index].BreakRun();
                continue;
            }

            ref var startTotal = ref (isNegative ? ref startNegative : ref startPositive);
            ref var endTotal = ref (isNegative ? ref endNegative : ref endPositive);
            var startBottom = startTotal;
            var endBottom = endTotal;
            startTotal = Saturate(startTotal + start);
            endTotal = Saturate(endTotal + end);
            layers[index].AddInterval(
                new StackedAreaPoint(firstX, startBottom, startTotal), new StackedAreaPoint(lastX, endBottom, endTotal), isNegative);
        }
    }

    private static double Accumulate(double value, ref double positive, ref double negative)
    {
        ref var total = ref (value < 0 ? ref negative : ref positive);
        total = Saturate(total + value);
        return total;
    }

    private static double GetCrossing(double first, double last)
    {
        if (first == 0 || last == 0 || Math.Sign(first) == Math.Sign(last))
        {
            return double.NaN;
        }

        var larger = Math.Max(Math.Abs(first), Math.Abs(last));
        var start = Math.Abs(first) / larger;
        return Math.Clamp(start / (start + (Math.Abs(last) / larger)), double.Epsilon, Math.BitDecrement(1d));
    }

    private static double Interpolate(double first, double last, double fraction)
    {
        return Math.Clamp(first * (1 - fraction) + last * fraction, Math.Min(first, last), Math.Max(first, last));
    }

    private static double Saturate(double value)
    {
        return Math.Clamp(value, -double.MaxValue, double.MaxValue);
    }
}
