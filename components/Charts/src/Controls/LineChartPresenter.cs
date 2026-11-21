// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class LineChartPresenter
{
    private readonly LineChart _owner;
    private readonly Canvas _plot;
    private readonly CartesianPlotPresenter _cartesian;
    private readonly TextBlock? _details;
    private readonly ChartInspectorPresenter _inspector;
    private readonly LineChartViewportInput _viewportInput;
    private readonly AccessibilitySettings _accessibility = new();
    private readonly UISettings _settings = new();
    private readonly List<PlottedPoint> _points = [];
    private readonly List<int> _columns = [];

    private readonly Line _guide = new()
    {
        IsHitTestVisible = false, StrokeDashArray = new DoubleCollection { 3, 3 }, Visibility = Visibility.Collapsed,
    };

    private readonly TranslateTransform _guideOffset = new();
    private readonly DispatcherQueueTimer _guideTimer;
    private readonly Canvas _markers = new() { IsHitTestVisible = false };
    private readonly Grid _selectionOverlay = new() { IsHitTestVisible = false };
    private readonly Rectangle _selectionFill = new() { Opacity = 0.09 };
    private readonly Canvas _selectionFence = new();
    private readonly Line _selectionStart = new() { StrokeThickness = 1.5 };
    private readonly Line _selectionEnd = new() { StrokeThickness = 1.5 };
    private readonly Rectangle _startGrip = new() { RadiusX = 3, RadiusY = 3 };
    private readonly Rectangle _endGrip = new() { RadiusX = 3, RadiusY = 3 };
    private LineChartViewport? _selectionPreview;
    private ChartScale? _renderedXAxis;
    private Rect _plotBounds;
    private LineChartData? _boundsData;
    private (double XMin, double XMax, double YMin, double YMax) _dataBounds;
    private bool _hasViewport;
    private bool _viewportRenderQueued;
    private bool _detached;
    private string? _plotMessage;
    private double _guideFrom;
    private double _guideTarget;
    private long _guideStartedAt;
    private TimeSpan _guideDuration;

    public LineChartPresenter(LineChart owner, Canvas plot, Grid? legend, TextBlock? details, Popup? popup)
    {
        this._owner = owner;
        this._plot = plot;
        this._cartesian = new CartesianPlotPresenter(owner, plot, legend);
        this._details = details;
        this._guide.RenderTransform = this._guideOffset;
        Canvas.SetZIndex(this._selectionOverlay, 3);
        Canvas.SetZIndex(this._guide, 4);
        Canvas.SetZIndex(this._markers, 5);
        this._selectionOverlay.Children.Add(this._selectionFill);
        this._selectionOverlay.Children.Add(this._selectionFence);
        this._selectionFence.Children.Add(this._selectionStart);
        this._selectionFence.Children.Add(this._selectionEnd);
        this._selectionFence.Children.Add(this._startGrip);
        this._selectionFence.Children.Add(this._endGrip);
        this._guideTimer = owner.DispatcherQueue.CreateTimer();
        this._guideTimer.Interval = TimeSpan.FromMilliseconds(16);
        this._guideTimer.IsRepeating = true;
        this._guideTimer.Tick += this.OnGuideTick;
        plot.SizeChanged += this.OnSizeChanged;
        this._viewportInput = new LineChartViewportInput(
            owner, plot, () => this._plotBounds, this.ClearInspection, this.GetRangeBetween,
            () => this.GetSelectionBounds(this._owner.SelectedRange), owner.SetSelectionPreview);
        this._inspector = new ChartInspectorPresenter(
            owner, plot, popup, () => this._columns.Count, this.HitTest, this.ReadColumn, this.HighlightColumn, details,
            readContext: this.ReadContext);
    }

    public void Detach()
    {
        this._detached = true;
        this._viewportInput.Detach();
        this._plot.SizeChanged -= this.OnSizeChanged;
        this._inspector.Detach();
        this.HideGuide();
        this._guideTimer.Tick -= this.OnGuideTick;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        this.Render();
    }

    public void CancelGesture()
    {
        this._viewportInput.CancelGesture();
    }

    public void CancelSelectionDrag()
    {
        this._viewportInput.CancelSelectionDrag();
    }

    public void RefreshViewportInput()
    {
        this._viewportInput.RefreshCursor();
    }

    public void QueueViewportRender()
    {
        if (!this._viewportRenderQueued)
        {
            this._viewportRenderQueued = this._owner.DispatcherQueue.TryEnqueue(
                DispatcherQueuePriority.Low, () =>
                {
                    if (this._viewportRenderQueued && !this._detached)
                    {
                        this.Render();
                    }
                });
        }
    }

    public void Render()
    {
        this.CancelSelectionDrag();
        this._viewportRenderQueued = false;
        this._hasViewport = false;
        this._plotBounds = default;
        this._renderedXAxis = null;
        this._viewportInput.RefreshCursor();
        this.HideGuide();
        this._plot.Children.Clear();
        this._cartesian.ClearSeriesVisuals();
        this._points.Clear();
        this._columns.Clear();
        this._markers.Children.Clear();
        this._plotMessage = null;
        var data = this._owner.Data;
        if (this._boundsData != data)
        {
            this._boundsData = data;
            this._dataBounds = data is null ? default : GetBounds(data);
        }

        var width = this._plot.ActualWidth;
        var height = this._plot.ActualHeight;
        if (data is null || data.Series.Count == 0)
        {
            this._cartesian.RebuildLegend(null, [], this._owner.ShowLegend);
            this.ClearInspection();
            return;
        }

        var highContrast = this._accessibility.HighContrast;
        var palette = ChartPalette.GetEffectiveColors(this._owner);
        var theme = this._owner.ActualTheme;
        var brushes = new SolidColorBrush[data.Series.Count];
        for (var index = 0; index < brushes.Length; index++)
        {
            var color = highContrast
                ? this._settings.UIElementColor(index % 2 == 0 ? UIElementType.WindowText : UIElementType.Highlight)
                : data.Series[index].LineColor ?? palette.GetColor(index, theme);
            brushes[index] = new SolidColorBrush(color);
        }

        this._cartesian.RebuildLegend(
            data.Series.Select(series => series.Name).ToArray(), brushes, this._owner.ShowLegend,
            ChartLegend.GetValueMode(this._owner) == ChartLegendValueMode.Value
                ? index => data.Series[index].Points.LastOrDefault(static point => point.Y.HasValue).Y is double value
                    ? FormatNumber(value, this._owner.ValueFormat)
                    : null
                : null, index => IsStrokeDashed(data.Series[index], index, highContrast));
        if (width < 20 || height < 20)
        {
            this.ClearInspection();
            return;
        }

        this._plot.Clip = new RectangleGeometry { Rect = new Rect(0, 0, width, height) };
        var stacked = this._owner.Mode == LineChartMode.StackedArea;
        var stack = stacked ? data.Stack : null;
        if (stacked && stack is null)
        {
            this._plotMessage = "Stacked area needs matching X coordinates in every series.";
            this.ClearInspection();
            return;
        }

        var (xMin, xMax, yMin, yMax) = this._dataBounds;
        if (!double.IsFinite(xMin))
        {
            this.ClearInspection();
            return;
        }

        var xIntervals = (int)Math.Clamp(width / 88, 2, 7);
        var yIntervals = (int)Math.Clamp(height / 38, 2, 6);
        var xOptions = this._owner.XAxis;
        var yOptions = this._owner.YAxis;
        ChartScale xScale;
        if (this._owner.VisibleViewport is { } viewport)
        {
            var (first, last) = viewport.GetBounds(data.TimeOriginTicks);
            this._hasViewport = first < last;
            xScale = data.IsTimeBased
                ? ChartScale.ForTime(first, last, xIntervals)
                : ChartScale.ForLine(first, last, xIntervals, roundBounds: false);
        }
        else
        {
            xScale = data.IsTimeBased ? ChartScale.ForTime(xMax, xIntervals) : ChartScale.ForAxis(xMin, xMax, xIntervals, xOptions);
        }

        var yScale = ChartScale.ForAxis(stack?.Minimum ?? yMin, stack?.Maximum ?? yMax, yIntervals, yOptions);
        var labelMinimum = this._hasViewport ? xScale.Minimum : 0;
        var labelMaximum = this._hasViewport ? xScale.Maximum : xMax;
        if (this._cartesian.DrawAxes(
                xScale, yScale, x => this.FormatAxisX(data, x, labelMinimum, labelMaximum),
                y => FormatNumber(y, yOptions?.TickLabelFormat ?? this._owner.ValueFormat), this._owner.ShowGrid, xOptions, yOptions) is not
            { } layout)
        {
            this.ClearInspection();
            return;
        }

        this._plotBounds = layout.Bounds;
        this._renderedXAxis = layout.X;
        for (var index = 0; index < data.Series.Count; index++)
        {
            if (stack is null)
            {
                this.DrawSeries(data, index, brushes[index], layout, highContrast);
            }
            else
            {
                this.DrawStackedSeries(data.Series[index], stack.Layers[index], index, brushes[index], layout, highContrast);
            }
        }

        long? timeOriginTicks = data.IsTimeBased ? data.TimeOriginTicks : null;
        this._cartesian.DrawReferenceLines(layout, xOptions, yOptions, highContrast, timeOriginTicks);
        ChartMilestoneRenderer.Draw(this._plot, this._owner, layout, this._owner.Milestones, highContrast, timeOriginTicks);

        this._points.Sort(static (left, right) =>
        {
            var order = left.Source.X is double x
                ? x.CompareTo(right.Source.X!.Value)
                : left.Source.Timestamp!.Value.CompareTo(right.Source.Timestamp!.Value);
            return order != 0 ? order : left.Series.CompareTo(right.Series);
        });
        for (var index = 0; index < this._points.Count; index++)
        {
            if (index == 0
                || this._points[index].Source.X != this._points[index - 1].Source.X
                || this._points[index].Source.Timestamp != this._points[index - 1].Source.Timestamp)
            {
                this._columns.Add(index);
            }
        }

        this._guide.Y1 = this._plotBounds.Top;
        this._guide.Y2 = this._plotBounds.Bottom;
        this._guide.Stroke = this._owner.Foreground;
        this._plot.Children.Add(this._guide);
        this._plot.Children.Add(this._markers);
        this.RefreshSelection();
        this.UpdateDetails(null);
        this._inspector.Refresh();
    }

    private LineChartViewport? GetRangeBetween(double firstX, double lastX)
    {
        if (this._renderedXAxis is not { } axis || this._owner.Data is not { } data || this._plotBounds.Width <= 0)
        {
            return null;
        }

        var first = Math.Clamp((Math.Min(firstX, lastX) - this._plotBounds.Left) / this._plotBounds.Width, 0, 1);
        var last = Math.Clamp((Math.Max(firstX, lastX) - this._plotBounds.Left) / this._plotBounds.Width, 0, 1);
        if (first >= last)
        {
            return null;
        }

        if (!data.IsTimeBased)
        {
            return new LineChartViewport(axis.Minimum, axis.Maximum).Slice(first, last);
        }

        var start = GetTimestamp(data.TimeOriginTicks, axis.Minimum);
        var end = GetTimestamp(data.TimeOriginTicks, axis.Maximum);
        return start < end ? new LineChartViewport(start, end).Slice(first, last) : null;
    }

    private Rect? GetSelectionBounds(LineChartViewport? range)
    {
        if (range is null || this._owner.Data is not { } data || this._renderedXAxis is not { } axis)
        {
            return null;
        }

        var (minimum, maximum) = range.GetBounds(data.TimeOriginTicks);
        var left = this._plotBounds.Left + axis.Normalize(minimum) * this._plotBounds.Width;
        var right = this._plotBounds.Left + axis.Normalize(maximum) * this._plotBounds.Width;
        return right > left ? new Rect(left, this._plotBounds.Top, right - left, this._plotBounds.Height) : null;
    }

    public void ShowSelectionPreview(LineChartViewport? range)
    {
        this._selectionPreview = range;
        this.RefreshSelection();
    }

    public void RefreshSelection()
    {
        this._viewportInput.RefreshCursor();
        if (this.GetSelectionBounds(this._selectionPreview ?? this._owner.SelectedRange) is not { } bounds)
        {
            this._selectionOverlay.Visibility = Visibility.Collapsed;
            return;
        }

        this._selectionFill.Fill = this._owner.Foreground;
        PlaceFence(this._selectionStart, this._startGrip, 0);
        PlaceFence(this._selectionEnd, this._endGrip, bounds.Width);
        this._selectionOverlay.Width = bounds.Width;
        this._selectionOverlay.Height = bounds.Height;
        Canvas.SetLeft(this._selectionOverlay, bounds.X);
        Canvas.SetTop(this._selectionOverlay, bounds.Y);
        this._selectionOverlay.Visibility = Visibility.Visible;
        if (!this._plot.Children.Contains(this._selectionOverlay))
        {
            this._plot.Children.Add(this._selectionOverlay);
        }

        void PlaceFence(Line fence, Rectangle grip, double x)
        {
            fence.Stroke = grip.Fill = this._owner.Foreground;
            fence.X1 = fence.X2 = x;
            fence.Y2 = bounds.Height;
            grip.Width = Math.Min(6, bounds.Width / 2);
            grip.Height = Math.Min(22, bounds.Height);
            Canvas.SetLeft(grip, x - grip.Width / 2);
            Canvas.SetTop(grip, (bounds.Height - grip.Height) / 2);
        }
    }

    public void ClearInspection()
    {
        this._inspector.Clear();
        this.UpdateDetails(this._plotMessage);
    }

    private void UpdateDetails(string? message)
    {
        if (this._details is not null)
        {
            this._details.Text = message ?? string.Empty;
            this._details.Visibility = message is null ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private int GetEnd(int column)
    {
        return column + 1 < this._columns.Count ? this._columns[column + 1] : this._points.Count;
    }

    private ChartInspectionItem? ReadColumn(int column)
    {
        var start = this._columns[column];
        var point = this._points[start];
        var x = point.Source.Timestamp is DateTimeOffset time
            ? FormatTime(time.ToUniversalTime(), this._owner.XValueFormat, "yyyy-MM-dd HH:mm:ss.FFFFFFF 'UTC'")
            : FormatNumber(point.Source.X!.Value, this._owner.XValueFormat);
        var end = this.GetEnd(column);
        var rows = new ChartInspectionRow[end - start + 1];
        rows[0] = new(x);
        for (var index = start; index < end; index++)
        {
            var sample = this._points[index];
            var series = this._owner.Data!.Series[sample.Series];
            rows[index - start + 1] = new($"{series.Name}: {FormatNumber(sample.Source.Y!.Value, this._owner.ValueFormat)}", sample.Brush);
        }

        return new ChartInspectionItem(point.Coordinate, rows);
    }

    private ChartContextValue[] ReadContext(int column)
    {
        var start = this._columns[column];
        var values = new ChartContextValue[this.GetEnd(column) - start];
        for (var index = 0; index < values.Length; index++)
        {
            var point = this._points[start + index];
            var series = this._owner.Data!.Series[point.Series];
            values[index] = new ChartContextValue(
                series.Name, point.Source.Y, point.Source, point.Series, X: point.Source.X, Timestamp: point.Source.Timestamp);
        }

        return values;
    }

    private void HighlightColumn(int column)
    {
        this._markers.Children.Clear();
        if (column < 0)
        {
            this.HideGuide();
            return;
        }

        var start = this._columns[column];
        var end = this.GetEnd(column);
        this.MoveGuide(this._points[start].Coordinate.X);
        for (var index = start; index < end; index++)
        {
            var point = this._points[index];
            var marker = new Ellipse
            {
                Width = 8,
                Height = 8,
                StrokeThickness = 1.5,
                Stroke = this._owner.Foreground,
                Fill = point.Brush,
            };
            Canvas.SetLeft(marker, point.Coordinate.X - marker.Width / 2);
            Canvas.SetTop(marker, point.Coordinate.Y - marker.Height / 2);
            this._markers.Children.Add(marker);
        }
    }

    private void MoveGuide(double target)
    {
        var visible = this._guide.Visibility == Visibility.Visible;
        if (visible && target == this._guideTarget)
        {
            return;
        }

        this._guideTarget = target;
        this._guideDuration = this._owner.InspectionTransitionDuration;
        if (!visible
            || this._guideDuration <= TimeSpan.Zero
            || !this._owner.AnimationsEnabled
            || Math.Abs(this._guideOffset.X - target) < 0.5)
        {
            this.FinishGuideTransition();
        }
        else
        {
            this._guideFrom = this._guideOffset.X;
            this._guideStartedAt = Stopwatch.GetTimestamp();
            if (!this._guideTimer.IsRunning)
            {
                this._guideTimer.Start();
            }
        }

        this._guide.Visibility = Visibility.Visible;
    }

    private void OnGuideTick(DispatcherQueueTimer sender, object args)
    {
        if (!sender.IsRunning)
        {
            return;
        }

        var remaining = 1 - Stopwatch.GetElapsedTime(this._guideStartedAt) / this._guideDuration;
        if (remaining <= 0)
        {
            this.FinishGuideTransition();
            return;
        }

        this._guideOffset.X = this._guideTarget + (this._guideFrom - this._guideTarget) * remaining * remaining * remaining;
    }

    public void FinishGuideTransition()
    {
        this._guideTimer.Stop();
        this._guideOffset.X = this._guideTarget;
    }

    private void HideGuide()
    {
        this._guideTimer.Stop();
        this._guide.Visibility = Visibility.Collapsed;
    }

    private int HitTest(Point position)
    {
        if (this._viewportInput.IsInteracting || this._columns.Count == 0 || !this._plotBounds.Contains(position))
        {
            return -1;
        }

        var low = 0;
        var high = this._columns.Count - 1;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (this._points[this._columns[middle]].Coordinate.X < position.X)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low > 0
               && position.X - this._points[this._columns[low - 1]].Coordinate.X
               < this._points[this._columns[low]].Coordinate.X - position.X
            ? low - 1
            : low;
    }

    private void DrawSeries(LineChartData data, int seriesIndex, SolidColorBrush brush, CartesianPlotLayout layout, bool highContrast)
    {
        var series = data.Series[seriesIndex];
        var line = new PathGeometry();
        var fill = (series.ShowAreaFill ?? this._owner.ShowAreaFill) ? new PathGeometry() : null;
        var isolated = new GeometryGroup();
        var run = new List<Point>();
        var first = 0;
        var end = series.Points.Count;
        if (this._hasViewport)
        {
            (first, end) = FindVisibleRange(series.Points, data.GetX, layout.X, neighbors: 2);
        }

        for (var index = first; index < end; index++)
        {
            var point = series.Points[index];
            if (point.Y is not double y)
            {
                FinishRun();
                continue;
            }

            var x = data.GetX(point);
            var coordinate = new Point(layout.MapX(x), layout.MapY(y));
            run.Add(coordinate);
            if (layout.X.Contains(x) && layout.Y.Contains(y))
            {
                this._points.Add(new PlottedPoint(coordinate, point, seriesIndex, brush));
            }
        }

        FinishRun();
        this.AddSeriesVisuals(series, seriesIndex, brush, line, fill, isolated, highContrast, stacked: false);

        void FinishRun()
        {
            if (run.Count == 1)
            {
                isolated.Children.Add(new EllipseGeometry { Center = run[0], RadiusX = 2.5, RadiusY = 2.5 });
            }
            else if (run.Count > 1)
            {
                var curve = this._owner.Interpolation == LineChartInterpolation.Monotone && run.Count > 2
                    ? MonotoneCurve.CreateSegments(run)
                    : null;
                AddRun(line, run, curve, baseline: null);
                if (fill is not null)
                {
                    var zero = layout.Bounds.Bottom - layout.Y.Normalize(0) * layout.Bounds.Height;
                    AddRun(fill, run, curve, zero);
                }
            }

            run.Clear();
        }
    }

    private void DrawStackedSeries(
        LineChartSeries series,
        StackedAreaLayer layer,
        int seriesIndex,
        SolidColorBrush brush,
        CartesianPlotLayout layout,
        bool highContrast)
    {
        var line = new PathGeometry();
        var fill = new PathGeometry();
        var isolated = new GeometryGroup();
        var smooth = this._owner.Interpolation == LineChartInterpolation.Monotone;
        foreach (var run in layer.Runs)
        {
            var (first, end) = this._hasViewport ? FindVisibleRange(run, static point => point.X, layout.X, neighbors: 1) : (0, run.Count);
            if (end <= first)
            {
                continue;
            }

            var boundary = new List<Point>(end - first);
            var baseline = new List<Point>(end - first);
            for (var index = first; index < end; index++)
            {
                var point = run[index];
                var x = layout.MapX(point.X);
                boundary.Add(new Point(x, layout.MapY(point.Boundary)));
                baseline.Add(new Point(x, layout.MapY(point.Baseline)));
            }

            var edge = new PathFigure { StartPoint = boundary[0], IsFilled = false };
            AddStackedBoundary(edge, boundary, smooth);
            line.Figures.Add(edge);

            var band = new PathFigure { StartPoint = boundary[0], IsClosed = true, IsFilled = true };
            AddStackedBoundary(band, boundary, smooth);
            baseline.Reverse();
            band.Segments.Add(new LineSegment { Point = baseline[0] });
            AddStackedBoundary(band, baseline, smooth);
            fill.Figures.Add(band);
        }

        var (start, last) = this._hasViewport
            ? FindVisibleRange(layer.Samples, static point => point.X, layout.X, neighbors: 0)
            : (0, layer.Samples.Count);
        for (var index = start; index < last; index++)
        {
            var sample = layer.Samples[index];
            if (!layout.X.Contains(sample.X) || !layout.Y.Contains(sample.Top))
            {
                continue;
            }

            var coordinate = new Point(layout.MapX(sample.X), layout.MapY(sample.Top));
            this._points.Add(new PlottedPoint(coordinate, sample.Source, seriesIndex, brush));
            if (sample.IsIsolated)
            {
                isolated.Children.Add(new EllipseGeometry { Center = coordinate, RadiusX = 2.5, RadiusY = 2.5 });
            }
        }

        this.AddSeriesVisuals(series, seriesIndex, brush, line, fill, isolated, highContrast, stacked: true);
    }

    private static (int First, int End) FindVisibleRange<T>(IReadOnlyList<T> points, Func<T, double> getX, ChartScale axis, int neighbors)
    {
        if (points.Count == 0 || getX(points[0]) > axis.Maximum || getX(points[^1]) < axis.Minimum)
        {
            return (0, 0);
        }

        var first = FindBound(axis.Minimum, includeEqual: false);
        var end = FindBound(axis.Maximum, includeEqual: true);
        return (Math.Max(0, first - neighbors), Math.Min(points.Count, end + neighbors));

        int FindBound(double value, bool includeEqual)
        {
            var low = 0;
            var high = points.Count;
            while (low < high)
            {
                var middle = low + (high - low) / 2;
                var x = getX(points[middle]);
                if (x < value || (includeEqual && x == value))
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }
    }

    private static void AddStackedBoundary(PathFigure figure, List<Point> points, bool smooth)
    {
        if (!smooth)
        {
            AddLinearSegments(figure, points, baseline: null);
            return;
        }

        // Every band uses the same easing so a shared boundary cannot drift.
        for (var index = 1; index < points.Count; index++)
        {
            var first = points[index - 1];
            var last = points[index];
            var third = (last.X - first.X) / 3;
            figure.Segments.Add(
                third == 0
                    ? new LineSegment { Point = last }
                    : new BezierSegment
                    {
                        Point1 = new Point(first.X + third, first.Y), Point2 = new Point(last.X - third, last.Y), Point3 = last,
                    });
        }
    }

    private static bool IsStrokeDashed(LineChartSeries series, int index, bool highContrast)
    {
        return highContrast ? index % 2 == 1 : series.IsStrokeDashed;
    }

    private void AddSeriesVisuals(
        LineChartSeries series,
        int seriesIndex,
        SolidColorBrush brush,
        PathGeometry line,
        PathGeometry? fill,
        GeometryGroup isolated,
        bool highContrast,
        bool stacked)
    {
        if (fill is not null && fill.Figures.Count > 0)
        {
            var color = brush.Color;
            var opacity = stacked ? 0.55 : 0.22;
            var fillColor = highContrast
                ? Color.FromArgb(45, color.R, color.G, color.B)
                : series.FillColor ?? Color.FromArgb((byte)(color.A * opacity), color.R, color.G, color.B);
            this.AddSeriesPath(new Path { Data = fill, Fill = new SolidColorBrush(fillColor) }, 1, highContrast, seriesIndex);
        }

        if (line.Figures.Count > 0)
        {
            var stroke = new Path
            {
                Data = line, Stroke = brush, StrokeThickness = series.StrokeThickness, StrokeLineJoin = PenLineJoin.Round
            };
            if (IsStrokeDashed(series, seriesIndex, highContrast))
            {
                stroke.StrokeDashArray = ChartLegendPresenter.CreateSeriesDashArray();
            }

            this.AddSeriesPath(stroke, 2, highContrast, seriesIndex);
        }

        if (isolated.Children.Count > 0)
        {
            this.AddSeriesPath(new Path { Data = isolated, Fill = brush }, 2, highContrast, seriesIndex);
        }
    }

    private void AddSeriesPath(Path path, int layer, bool highContrast, int seriesIndex)
    {
        path.Stretch = Stretch.None;
        path.IsHitTestVisible = false;
        if (this._hasViewport
            || this._owner.XAxis?.Minimum is not null
            || this._owner.XAxis?.Maximum is not null
            || this._owner.YAxis?.Minimum is not null
            || this._owner.YAxis?.Maximum is not null)
        {
            path.Clip = new RectangleGeometry { Rect = this._plotBounds };
        }

        if (highContrast)
        {
            path.HighContrastAdjustment = ElementHighContrastAdjustment.None;
        }

        Canvas.SetZIndex(path, layer);
        this._plot.Children.Add(path);
        this._cartesian.TrackSeriesVisual(path, seriesIndex);
    }

    private static void AddRun(PathGeometry geometry, List<Point> run, MonotoneSegment[]? curve, double? baseline)
    {
        var figure = new PathFigure
        {
            StartPoint = baseline is double y ? new Point(run[0].X, y) : run[0],
            IsClosed = baseline.HasValue,
            IsFilled = baseline.HasValue,
        };
        if (curve is null)
        {
            AddLinearSegments(figure, run, baseline);
        }
        else
        {
            AddSmoothSegments(figure, run[0], curve, baseline);
        }

        geometry.Figures.Add(figure);
    }

    private static void AddLinearSegments(PathFigure figure, List<Point> run, double? baseline)
    {
        var segment = new PolyLineSegment();
        if (baseline.HasValue)
        {
            segment.Points.Add(run[0]);
        }

        for (var index = 1; index < run.Count; index++)
        {
            segment.Points.Add(run[index]);
        }

        if (baseline is double bottom)
        {
            segment.Points.Add(new Point(run[^1].X, bottom));
        }

        figure.Segments.Add(segment);
    }

    private static void AddSmoothSegments(PathFigure figure, Point start, MonotoneSegment[] curve, double? baseline)
    {
        if (baseline.HasValue)
        {
            figure.Segments.Add(new LineSegment { Point = start });
        }

        foreach (var segment in curve)
        {
            figure.Segments.Add(
                segment.IsVertical
                    ? new LineSegment { Point = segment.End }
                    : new BezierSegment { Point1 = segment.Control1, Point2 = segment.Control2, Point3 = segment.End });
        }

        if (baseline is double bottom)
        {
            figure.Segments.Add(new LineSegment { Point = new Point(curve[^1].End.X, bottom) });
        }
    }


    private string FormatAxisX(LineChartData data, double x, double minimum, double maximum)
    {
        var tickFormat = this._owner.XAxis?.TickLabelFormat ?? this._owner.XValueFormat;
        if (!data.IsTimeBased)
        {
            return FormatNumber(x, tickFormat);
        }

        var time = GetTimestamp(data.TimeOriginTicks, x);
        var start = GetTimestamp(data.TimeOriginTicks, minimum);
        var end = GetTimestamp(data.TimeOriginTicks, maximum);
        var duration = maximum - minimum;
        var format = start.Date == end.Date ? duration switch
            {
                < 0.001 => "HH:mm:ss.fffffff",
                < 1 => "HH:mm:ss.fff",
                < 3600 => "HH:mm:ss",
                _ => "HH:mm",
            } :
            duration < 7 * 86400 ? "MMM d HH:mm" : "MMM d yyyy";
        return FormatTime(time, tickFormat, format);
    }

    private static DateTimeOffset GetTimestamp(long origin, double seconds)
    {
        var ticks = origin + (long)Math.Round(seconds * TimeSpan.TicksPerSecond);
        return new DateTimeOffset(Math.Clamp(ticks, DateTimeOffset.MinValue.Ticks, DateTimeOffset.MaxValue.Ticks), TimeSpan.Zero);
    }

    private static string FormatTime(DateTimeOffset time, string? format, string fallback)
    {
        try
        {
            return time.ToString(format ?? fallback, CultureInfo.CurrentCulture);
        }
        catch (FormatException)
        {
            return time.ToString(fallback, CultureInfo.CurrentCulture);
        }
    }

    private static string FormatNumber(double value, string? format)
    {
        try
        {
            return value.ToString(format ?? "G6", CultureInfo.CurrentCulture);
        }
        catch (FormatException)
        {
            return value.ToString("G6", CultureInfo.CurrentCulture);
        }
    }

    private static (double XMin, double XMax, double YMin, double YMax) GetBounds(LineChartData data)
    {
        var xMin = double.PositiveInfinity;
        var xMax = double.NegativeInfinity;
        var yMin = double.PositiveInfinity;
        var yMax = double.NegativeInfinity;
        foreach (var series in data.Series)
        {
            if (series.Points.Count == 0)
            {
                continue;
            }

            xMin = Math.Min(xMin, data.GetX(series.Points[0]));
            xMax = Math.Max(xMax, data.GetX(series.Points[^1]));
            foreach (var point in series.Points)
            {
                if (point.Y is double y)
                {
                    yMin = Math.Min(yMin, y);
                    yMax = Math.Max(yMax, y);
                }
            }
        }

        return double.IsFinite(yMin) ? (xMin, xMax, yMin, yMax) : (xMin, xMax, 0, 1);
    }

    private readonly record struct PlottedPoint(Point Coordinate, LineChartPoint Source, int Series, Brush Brush);
}
