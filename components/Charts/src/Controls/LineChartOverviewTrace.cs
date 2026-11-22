// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace JPSoftworks.DesktopToolkit.Charts;

internal static class LineChartOverviewTrace
{
    public static void Draw(LineChart target, Canvas track, bool highContrast, UISettings settings)
    {
        if (target.Data is not { } data || target.NavigationExtent is not { } extent || track.ActualWidth < 1 || track.ActualHeight < 1)
        {
            return;
        }

        var stack = target.Mode == LineChartMode.StackedArea ? data.Stack : null;
        var (minimumY, maximumY) = stack is null ? GetYBounds(data) : (stack.Minimum, stack.Maximum);
        if (minimumY > maximumY)
        {
            return;
        }

        var (minimumX, maximumX) = extent.GetBounds(data.TimeOriginTicks);
        var palette = ChartPalette.GetEffectiveColors(target);
        var columns = (int)Math.Ceiling(track.ActualWidth);
        for (var index = 0; index < data.Series.Count; index++)
        {
            var trace = new TraceBuilder(columns);
            if (stack is null)
            {
                foreach (var point in data.Series[index].Points)
                {
                    if (point.Y is double y)
                    {
                        Add(point, y);
                    }
                    else
                    {
                        trace.Break();
                    }
                }
            }
            else
            {
                foreach (var run in stack.Layers[index].Runs)
                {
                    trace.Break();
                    foreach (var point in run)
                    {
                        AddPoint(point.X, point.Boundary);
                    }
                }

                foreach (var sample in stack.Layers[index].Samples)
                {
                    if (sample.IsIsolated)
                    {
                        trace.Break();
                        Add(sample.Source, sample.Top);
                    }
                }
            }

            var color = highContrast
                ? settings.UIElementColor(index % 2 == 0 ? UIElementType.WindowText : UIElementType.Highlight)
                : data.Series[index].LineColor ?? palette.GetColor(index, target.ActualTheme);
            track.Children.Add(
                new Path
                {
                    Data = trace.Build(),
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = 1.25,
                    IsHitTestVisible = false,
                    Stretch = Stretch.None,
                });

            void Add(LineChartPoint point, double y)
            {
                AddPoint(data.GetX(point), y);
            }

            void AddPoint(double x, double y)
            {
                var scaledX = ChartScale.Normalize(x, minimumX, maximumX) * track.ActualWidth;
                var scaledY = (1 - ChartScale.Normalize(y, minimumY, maximumY)) * Math.Max(0, track.ActualHeight - 2) + 1;
                trace.Add(new Point(scaledX, scaledY));
            }
        }
    }

    private static (double Minimum, double Maximum) GetYBounds(LineChartData data)
    {
        var minimum = double.PositiveInfinity;
        var maximum = double.NegativeInfinity;
        foreach (var series in data.Series)
        {
            foreach (var point in series.Points)
            {
                if (point.Y is double y)
                {
                    minimum = Math.Min(minimum, y);
                    maximum = Math.Max(maximum, y);
                }
            }
        }

        return (minimum, maximum);
    }

    // A pixel column keeps the extrema and any connection to the previous column.
    private sealed class TraceBuilder(int columns)
    {
        private readonly Bucket[] _buckets = new Bucket[columns];
        private int _previousColumn = -1;
        private Point _previousPoint;

        public void Break()
        {
            this._previousColumn = -1;
        }

        public void Add(Point point)
        {
            var column = Math.Min(this._buckets.Length - 1, (int)point.X);
            ref var bucket = ref this._buckets[column];
            if (!bucket.HasValue)
            {
                bucket.HasValue = true;
                bucket.First = bucket.Low = bucket.High = point;
                if (this._previousColumn >= 0 && this._previousColumn != column)
                {
                    bucket.Previous = this._previousPoint;
                    this._buckets[this._previousColumn].HasOutgoing = true;
                }
            }
            else
            {
                if (point.Y < bucket.Low.Y)
                {
                    bucket.Low = point;
                }

                if (point.Y > bucket.High.Y)
                {
                    bucket.High = point;
                }
            }

            this._previousColumn = column;
            this._previousPoint = point;
        }

        public PathGeometry Build()
        {
            var geometry = new PathGeometry();
            foreach (var bucket in this._buckets)
            {
                if (!bucket.HasValue)
                {
                    continue;
                }

                if (bucket.Previous is { } previous)
                {
                    DrawSegment(previous, bucket.First);
                }

                if (bucket.High.Y != bucket.Low.Y)
                {
                    DrawSegment(bucket.Low, bucket.High);
                }
                else if (bucket.Previous is null && !bucket.HasOutgoing)
                {
                    DrawSegment(new Point(bucket.First.X - 0.5, bucket.First.Y), new Point(bucket.First.X + 0.5, bucket.First.Y));
                }
            }

            return geometry;

            void DrawSegment(Point start, Point end)
            {
                var figure = new PathFigure { StartPoint = start, IsFilled = false };
                figure.Segments.Add(new LineSegment { Point = end });
                geometry.Figures.Add(figure);
            }
        }

        private struct Bucket
        {
            public bool HasValue;
            public bool HasOutgoing;
            public Point First;
            public Point Low;
            public Point High;
            public Point? Previous;
        }
    }
}
