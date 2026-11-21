// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>A snapshot of line series sharing a numeric or time X axis.</summary>
public sealed class LineChartData
{
    private readonly Lazy<LineChartStack?> _stack;

    /// <summary>Gets the series in drawing and legend order.</summary>
    public IReadOnlyList<LineChartSeries> Series { get; }

    /// <summary>Gets whether the X axis represents timestamps.</summary>
    public bool IsTimeBased { get; }

    /// <summary>Gets the earliest and latest X values. Null indicates fewer than two distinct X values.</summary>
    public LineChartViewport? Extent { get; }

    internal long TimeOriginTicks { get; }

    internal LineChartStack? Stack => this._stack.Value;

    /// <summary>Takes a snapshot of the series. Each series can have its own X coordinates.</summary>
    public LineChartData(LineChartSeries[] series)
    {
        ArgumentNullException.ThrowIfNull(series);

        var snapshot = (LineChartSeries[])series.Clone();
        bool? timeBased = null;
        var origin = long.MaxValue;
        var last = long.MinValue;
        var minimum = double.PositiveInfinity;
        var maximum = double.NegativeInfinity;
        foreach (var item in snapshot)
        {
            if (item is null)
            {
                throw new ArgumentException("Series cannot be null.", nameof(series));
            }

            if (item.Points.Count == 0)
            {
                continue;
            }

            var timestamp = item.Points[0].Timestamp;
            if (timeBased.HasValue && timeBased.Value != timestamp.HasValue)
            {
                throw new ArgumentException("All series must use the same X axis type.", nameof(series));
            }

            timeBased = timestamp.HasValue;
            if (timestamp is DateTimeOffset instant)
            {
                origin = Math.Min(origin, instant.UtcTicks);
                last = Math.Max(last, item.Points[^1].Timestamp!.Value.UtcTicks);
            }
            else
            {
                minimum = Math.Min(minimum, item.Points[0].X!.Value);
                maximum = Math.Max(maximum, item.Points[^1].X!.Value);
            }
        }

        this.Series = Array.AsReadOnly(snapshot);
        this.IsTimeBased = timeBased == true;
        this.TimeOriginTicks = this.IsTimeBased ? origin : 0;
        this.Extent = this.IsTimeBased
            ?
            origin < last ? new LineChartViewport(new DateTimeOffset(origin, TimeSpan.Zero), new DateTimeOffset(last, TimeSpan.Zero)) : null
            : minimum < maximum
                ? new LineChartViewport(minimum, maximum)
                : null;
        this._stack = new Lazy<LineChartStack?>(() => this.Series.Count == 0 ? null : LineChartStack.Create(this));
    }

    internal double GetX(LineChartPoint point)
    {
        return point.Timestamp is DateTimeOffset timestamp
            ? (timestamp.UtcTicks - this.TimeOriginTicks) / (double)TimeSpan.TicksPerSecond
            : point.X!.Value;
    }
}
