// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class ChartScale
{
    private readonly double _minimum;
    private readonly double _maximum;

    public static ChartScale Percent { get; } = new(0, 1, (double[])[0, 0.25, 0.5, 0.75, 1]);

    public IReadOnlyList<double> Ticks { get; }

    public double Minimum => this._minimum;

    public double Maximum => this._maximum;

    private ChartScale(double minimum, double maximum, IReadOnlyList<double> ticks)
    {
        this._minimum = minimum;
        this._maximum = maximum;
        this.Ticks = ticks;
    }

    public bool Contains(double value)
    {
        return value >= this._minimum && value <= this._maximum;
    }

    public double Project(double value)
    {
        var span = this._maximum - this._minimum;
        var scaled = double.IsFinite(span)
            ? (value - this._minimum) / span
            : (value / 2 - this._minimum / 2) / (this._maximum / 2 - this._minimum / 2);
        if (double.IsInfinity(scaled) && double.IsFinite(span))
        {
            scaled = (value / 2 - this._minimum / 2) / (span / 2);
        }

        return Math.Clamp(scaled, -100_000, 100_000);
    }

    public double Normalize(double value)
    {
        return Normalize(value, this._minimum, this._maximum);
    }

    public static double Normalize(double value, double minimum, double maximum)
    {
        if (minimum == maximum)
        {
            return 0.5;
        }

        var clamped = Math.Clamp(value, minimum, maximum);
        var span = maximum - minimum;
        return double.IsFinite(span) ? (clamped - minimum) / span : (clamped / 2 - minimum / 2) / (maximum / 2 - minimum / 2);
    }

    public static ChartScale ForBars(double dataMinimum, double dataMaximum, double minimum, double maximum, int intervals)
    {
        var autoMinimum = !double.IsFinite(minimum) || minimum > 0;
        var autoMaximum = !double.IsFinite(maximum) || maximum < 0;
        minimum = autoMinimum ? dataMinimum : minimum;
        maximum = autoMaximum ? dataMaximum : maximum;
        if (minimum == maximum)
        {
            maximum = 1;
        }

        return Create(minimum, maximum, intervals, autoMinimum, autoMaximum);
    }

    public static ChartScale ForLine(double minimum, double maximum, int intervals, bool roundBounds = true)
    {
        if (minimum < maximum)
        {
            return Create(minimum, maximum, intervals, roundBounds, roundBounds);
        }

        var padding = Math.Max(1, Math.Abs(minimum) * 0.05);
        return new ChartScale(
            Math.Max(-double.MaxValue, minimum - padding), Math.Min(double.MaxValue, maximum + padding), (double[])[minimum]);
    }

    public static ChartScale ForAxis(double minimum, double maximum, int intervals, ChartAxisOptions? options)
    {
        if (options?.Minimum is null && options?.Maximum is null)
        {
            return ForLine(minimum, maximum, intervals);
        }

        var lower = options.Minimum ?? minimum;
        var upper = options.Maximum ?? maximum;
        if (lower >= upper)
        {
            if (options.Minimum.HasValue)
            {
                upper = Math.Min(double.MaxValue, lower + Math.Max(1, Math.Abs(lower) * 0.05));
            }
            else
            {
                lower = Math.Max(-double.MaxValue, upper - Math.Max(1, Math.Abs(upper) * 0.05));
            }
        }

        return Create(lower, upper, intervals, options.Minimum is null, options.Maximum is null);
    }

    public static ChartScale ForTime(double duration, int intervals)
    {
        return ForTime(0, duration, intervals);
    }

    public static ChartScale ForTime(double minimum, double maximum, int intervals)
    {
        if (maximum <= minimum)
        {
            return ForLine(minimum, minimum, intervals);
        }

        ReadOnlySpan<double> steps =
        [
            0.0000001, 0.000001, 0.00001, 0.0001, 0.001, 0.01, 0.1,
            1, 2, 5, 10, 15, 30, 60, 120, 300, 600, 900, 1800, 3600, 7200,
            14400, 21600, 43200, 86400, 172800, 604800, 1209600, 2419200,
        ];
        var target = (maximum - minimum) / intervals;
        foreach (var step in steps)
        {
            if (step >= target)
            {
                return CreateTicks(minimum, maximum, step, intervals);
            }
        }

        return ForLine(minimum, maximum, intervals, roundBounds: false);
    }

    private static ChartScale Create(double minimum, double maximum, int intervals, bool roundMinimum, bool roundMaximum)
    {
        var span = maximum - minimum;
        var roughStep = double.IsFinite(span) ? span / intervals : (maximum / intervals) - (minimum / intervals);
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(roughStep)));
        var scaledStep = roughStep / magnitude;
        var step = magnitude
                   * (scaledStep switch
                   {
                       <= 1 => 1,
                       <= 2 => 2,
                       <= 2.5 => 2.5,
                       <= 5 => 5,
                       _ => 10,
                   });
        if (!double.IsFinite(step) || step <= 0)
        {
            return new ChartScale(minimum, maximum, Endpoints(minimum, maximum));
        }

        var roundedMinimum = Math.Floor(minimum / step) * step;
        var roundedMaximum = Math.Ceiling(maximum / step) * step;
        if (roundMinimum && double.IsFinite(roundedMinimum))
        {
            minimum = Math.Min(minimum, roundedMinimum);
        }

        if (roundMaximum && double.IsFinite(roundedMaximum))
        {
            maximum = Math.Max(maximum, roundedMaximum);
        }

        return CreateTicks(minimum, maximum, step, intervals);
    }

    private static ChartScale CreateTicks(double minimum, double maximum, double step, int intervals)
    {
        var ticks = new List<double> { minimum };
        var first = Math.Ceiling(minimum / step);
        for (var index = 0; index <= intervals + 2; index++)
        {
            var tick = (first + index) * step;
            if (tick >= maximum)
            {
                break;
            }

            if (tick > ticks[^1])
            {
                ticks.Add(tick);
            }
        }

        ticks.Add(maximum);
        return new ChartScale(minimum, maximum, ticks);
    }

    private static double[] Endpoints(double minimum, double maximum)
    {
        return minimum < 0 && maximum > 0 ? [minimum, 0, maximum] : [minimum, maximum];
    }
}
