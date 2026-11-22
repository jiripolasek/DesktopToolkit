// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Defines the visible X range of a numeric or time-based line chart.</summary>
[WinRT.GeneratedBindableCustomProperty]
public sealed partial record LineChartViewport
{
    /// <summary>Gets the left bound of a numeric viewport.</summary>
    public double? Minimum { get; }

    /// <summary>Gets the right bound of a numeric viewport.</summary>
    public double? Maximum { get; }

    /// <summary>Gets the UTC start of a time viewport.</summary>
    public DateTimeOffset? Start { get; }

    /// <summary>Gets the UTC end of a time viewport.</summary>
    public DateTimeOffset? End { get; }

    /// <summary>Gets whether the viewport contains timestamps.</summary>
    public bool IsTimeBased => this.Start.HasValue;

    /// <summary>Creates a numeric viewport with finite, increasing bounds.</summary>
    public LineChartViewport(double minimum, double maximum)
    {
        if (!double.IsFinite(minimum) || !double.IsFinite(maximum) || minimum >= maximum)
        {
            throw new ArgumentException("Viewport bounds must be finite and increasing.");
        }

        this.Minimum = minimum;
        this.Maximum = maximum;
    }

    /// <summary>Creates a time viewport with increasing timestamps.</summary>
    public LineChartViewport(DateTimeOffset start, DateTimeOffset end)
    {
        if (start >= end)
        {
            throw new ArgumentException("Viewport timestamps must be increasing.");
        }

        this.Start = start.ToUniversalTime();
        this.End = end.ToUniversalTime();
    }

    internal LineChartViewport Union(LineChartViewport other)
    {
        if (this.IsTimeBased != other.IsTimeBased)
        {
            return this;
        }

        if (this.IsTimeBased)
        {
            var start = this.Start!.Value <= other.Start!.Value ? this.Start.Value : other.Start.Value;
            var end = this.End!.Value >= other.End!.Value ? this.End.Value : other.End.Value;
            return start == this.Start && end == this.End ? this : new LineChartViewport(start, end);
        }

        var minimum = Math.Min(this.Minimum!.Value, other.Minimum!.Value);
        var maximum = Math.Max(this.Maximum!.Value, other.Maximum!.Value);
        return minimum == this.Minimum && maximum == this.Maximum ? this : new LineChartViewport(minimum, maximum);
    }

    internal (double Min, double Max) GetBounds(long origin)
    {
        return this.Start is { } start
            ? ((start.UtcTicks - origin) / (double)TimeSpan.TicksPerSecond,
                (this.End!.Value.UtcTicks - origin) / (double)TimeSpan.TicksPerSecond)
            : (this.Minimum!.Value, this.Maximum!.Value);
    }

    internal (double Start, double End) GetFractions(LineChartViewport viewport)
    {
        var origin = this.Start?.UtcTicks ?? 0;
        var (min, max) = this.GetBounds(origin);
        var (first, last) = viewport.GetBounds(origin);
        return (ChartScale.Normalize(first, min, max), ChartScale.Normalize(last, min, max));
    }

    internal LineChartViewport? Slice(double first, double last)
    {
        if (this.Start is { } start)
        {
            var span = this.End!.Value.UtcTicks - start.UtcTicks;
            var minimum = start.AddTicks(Math.Clamp((long)Math.Round(span * first), 0, span));
            var maximum = start.AddTicks(Math.Clamp((long)Math.Round(span * last), 0, span));
            return minimum < maximum ? new LineChartViewport(minimum, maximum) : null;
        }

        var lower = Interpolate(this.Minimum!.Value, this.Maximum!.Value, first);
        var upper = Interpolate(this.Minimum.Value, this.Maximum.Value, last);
        return lower < upper ? new LineChartViewport(lower, upper) : null;
    }

    private static double Interpolate(double minimum, double maximum, double fraction)
    {
        return double.IsFinite(maximum - minimum)
            ? fraction <= 0.5 ? minimum + (maximum - minimum) * fraction : maximum - (maximum - minimum) * (1 - fraction)
            : minimum * (1 - fraction) + maximum * fraction;
    }
}
