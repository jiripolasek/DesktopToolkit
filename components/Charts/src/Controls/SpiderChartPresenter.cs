// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

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

internal sealed class SpiderChartPresenter
{
    private readonly SpiderChart _owner;
    private readonly Canvas _plot;
    private readonly CartesianPlotPresenter _cartesian;
    private readonly ChartInspectorPresenter _inspector;
    private readonly AccessibilitySettings _accessibility = new();
    private readonly UISettings _settings = new();
    private readonly Line _selection = new() { IsHitTestVisible = false, StrokeThickness = 2 };
    private Point _center;
    private Point[] _spokes = [];
    private SolidColorBrush[] _seriesBrushes = [];
    private double _radius;

    public SpiderChartPresenter(SpiderChart owner, Canvas plot, Grid? legend, Popup? popup, TextBlock? details)
    {
        this._owner = owner;
        this._plot = plot;
        this._cartesian = new CartesianPlotPresenter(owner, plot, legend);
        this._inspector = new ChartInspectorPresenter(
            owner, plot, popup, () => this._spokes.Length, this.HitTest, this.ReadAxis, this.Highlight, details,
            readContext: this.ReadContext);
        plot.SizeChanged += this.OnSizeChanged;
    }

    public void Detach()
    {
        this._plot.SizeChanged -= this.OnSizeChanged;
        this._inspector.Detach();
    }

    public void ClearInspection()
    {
        this._inspector.Clear();
    }

    public void Render()
    {
        this._plot.Children.Clear();
        this._cartesian.ClearSeriesVisuals();
        this._spokes = [];
        if (this._owner.Data is not { } data)
        {
            this._seriesBrushes = [];
            this._cartesian.RebuildLegend(null, [], false);
            return;
        }

        var highContrast = this._accessibility.HighContrast;
        var palette = ChartPalette.GetEffectiveColors(this._owner);
        var theme = this._owner.ActualTheme;
        var brushes = this._seriesBrushes = new SolidColorBrush[data.Series.Count];
        var names = new string[data.Series.Count];
        for (var index = 0; index < data.Series.Count; index++)
        {
            var series = data.Series[index];
            var color = highContrast
                ? this._settings.UIElementColor(index % 2 == 0 ? UIElementType.WindowText : UIElementType.Highlight)
                : series.LineColor ?? palette.GetColor(index, theme);
            brushes[index] = new SolidColorBrush(color);
            names[index] = series.Name;
        }

        this._cartesian.RebuildLegend(names, brushes, this._owner.ShowLegend && names.Length > 0);
        var width = this._plot.ActualWidth;
        var height = this._plot.ActualHeight;
        var axis = this._owner.AxisOptions;
        var presentation = ChartAxisAppearance.GetPresentation(this._owner);
        var showLabels = presentation != ChartAxisPresentation.Hidden && axis?.ShowLabels != false;
        var labelWidth = showLabels ? Math.Min(width * 0.21, 140) : 0;
        var labelHeight = showLabels ? Math.Min(this._owner.FontSize * 1.5, height * 0.16) : 0;
        this._center = new Point(width / 2, height / 2);
        this._radius = Math.Min(width / 2 - labelWidth - 6, height / 2 - labelHeight - 7);
        if (this._radius < 8)
        {
            this.ClearInspection();
            return;
        }

        this._plot.Clip = new RectangleGeometry { Rect = new Rect(0, 0, width, height) };
        this._spokes = new Point[data.Axes.Count];
        for (var index = 0; index < this._spokes.Length; index++)
        {
            var angle = Math.Tau * index / this._spokes.Length;
            this._spokes[index] = new Point(Math.Sin(angle), -Math.Cos(angle));
        }

        var showRings = presentation == ChartAxisPresentation.Standard && axis?.ShowGridLines != false;
        var showSpokes = presentation == ChartAxisPresentation.Standard && axis?.ShowAxisLine != false;
        if (showRings || showSpokes)
        {
            this.DrawGrid(highContrast, showRings, showSpokes);
        }

        for (var index = 0; index < data.Series.Count; index++)
        {
            this.DrawSeries(data, index, brushes[index], highContrast);
        }

        if (showLabels)
        {
            this.DrawLabels(data, labelWidth, height, width, axis?.LabelOpacity ?? 1);
        }

        this._selection.Stroke = this._owner.Foreground;
        this.Highlight(-1);
        this._plot.Children.Add(this._selection);
        this._inspector.Refresh();
    }

    private void DrawGrid(bool highContrast, bool showRings, bool showSpokes)
    {
        var geometry = new PathGeometry();
        if (showRings)
        {
            var ringCount = Math.Clamp(this._owner.RingCount, 1, 12);
            var vertices = new Point[this._spokes.Length];
            for (var ring = 1; ring <= ringCount; ring++)
            {
                for (var axis = 0; axis < vertices.Length; axis++)
                {
                    vertices[axis] = this.Position(axis, this._radius * ring / ringCount);
                }

                geometry.Figures.Add(Polygon(vertices, filled: false));
            }
        }

        if (showSpokes)
        {
            for (var axis = 0; axis < this._spokes.Length; axis++)
            {
                geometry.Figures.Add(Segment(this._center, this.Position(axis, this._radius)));
            }
        }

        var lineBrush = highContrast ? this._owner.Foreground : ChartAxisAppearance.GetLineBrush(this._owner);
        this._plot.Children.Add(
            new Path
            {
                Data = geometry,
                Stroke = lineBrush ?? this._owner.Foreground,
                Opacity = lineBrush is null ? 0.23 : 1,
                Stretch = Stretch.None,
                IsHitTestVisible = false,
            });
    }

    private void DrawSeries(SpiderChartData data, int index, SolidColorBrush brush, bool highContrast)
    {
        var series = data.Series[index];
        var vertices = new Point?[data.Axes.Count];
        var markers = new GeometryGroup();
        var complete = true;
        for (var axis = 0; axis < vertices.Length; axis++)
        {
            if (series.Values[axis] is not double value)
            {
                complete = false;
                continue;
            }

            var dimension = data.Axes[axis];
            var fraction = ChartScale.Normalize(value, dimension.Minimum, dimension.Maximum);
            var point = this.Position(axis, this._radius * fraction);
            vertices[axis] = point;
            markers.Children.Add(new EllipseGeometry { Center = point, RadiusX = 3, RadiusY = 3 });
        }

        var geometry = new PathGeometry { FillRule = FillRule.Nonzero };
        var fill = this._owner.ShowAreaFill && complete && !highContrast;
        if (complete)
        {
            geometry.Figures.Add(Polygon(Array.ConvertAll(vertices, static point => point!.Value), fill));
        }
        else
        {
            for (var axis = 0; axis < vertices.Length; axis++)
            {
                if (vertices[axis] is { } start && vertices[(axis + 1) % vertices.Length] is { } end)
                {
                    geometry.Figures.Add(Segment(start, end));
                }
            }
        }

        var color = brush.Color;
        var path = new Path
        {
            Data = geometry,
            Stroke = brush,
            StrokeThickness = 2,
            Stretch = Stretch.None,
            IsHitTestVisible = false,
            Fill = fill ? new SolidColorBrush(series.FillColor ?? Color.FromArgb(42, color.R, color.G, color.B)) : null,
            HighContrastAdjustment = highContrast ? ElementHighContrastAdjustment.None : ElementHighContrastAdjustment.Application,
        };
        if (highContrast && index % 2 != 0)
        {
            path.StrokeDashArray = [3, 2];
        }

        this._plot.Children.Add(path);
        this._cartesian.TrackSeriesVisual(path, index);
        var markerPath = new Path
        {
            Data = markers,
            Fill = brush,
            Stretch = Stretch.None,
            IsHitTestVisible = false,
            HighContrastAdjustment = path.HighContrastAdjustment,
        };
        this._plot.Children.Add(markerPath);
        this._cartesian.TrackSeriesVisual(markerPath, index);
    }

    private void DrawLabels(SpiderChartData data, double labelWidth, double height, double width, double opacity)
    {
        for (var axis = 0; axis < this._spokes.Length; axis++)
        {
            var endpoint = this.Position(axis, this._radius + 6);
            var label = new TextBlock
            {
                Text = data.Axes[axis].Name,
                Foreground = ChartAxisAppearance.GetLabelBrush(this._owner) ?? this._owner.Foreground,
                FontFamily = this._owner.FontFamily,
                FontSize = this._owner.FontSize,
                TextTrimming = TextTrimming.CharacterEllipsis,
                IsHitTestVisible = false,
                MaxWidth = labelWidth,
                Opacity = opacity,
            };
            label.Measure(new Size(labelWidth, height));
            var size = label.DesiredSize;
            var direction = this._spokes[axis];
            var x = endpoint.X - size.Width * (direction.X < -0.15 ? 1 : direction.X > 0.15 ? 0 : 0.5);
            var y = endpoint.Y - size.Height * (direction.Y < -0.85 ? 1 : direction.Y > 0.85 ? 0 : 0.5);
            Canvas.SetLeft(label, Math.Clamp(x, 0, Math.Max(0, width - size.Width)));
            Canvas.SetTop(label, Math.Clamp(y, 0, Math.Max(0, height - size.Height)));
            this._plot.Children.Add(label);
        }
    }

    private int HitTest(Point pointer)
    {
        var dx = pointer.X - this._center.X;
        var dy = pointer.Y - this._center.Y;
        if (this._spokes.Length == 0 || dx * dx + dy * dy > (this._radius + 10) * (this._radius + 10))
        {
            return -1;
        }

        return ((int)Math.Round((Math.Atan2(dx, -dy) + Math.Tau) * this._spokes.Length / Math.Tau)) % this._spokes.Length;
    }

    private ChartInspectionItem? ReadAxis(int index)
    {
        if (index < 0 || index >= this._spokes.Length || this._owner.Data is not { } data)
        {
            return null;
        }

        var axis = data.Axes[index];
        var rows = new ChartInspectionRow[data.Series.Count + 1];
        rows[0] = new(axis.Name);
        for (var series = 0; series < data.Series.Count; series++)
        {
            var item = data.Series[series];
            var value = item.Values[index] is double sample ? ChartInspectorPresenter.FormatValue(sample, axis.ValueFormat) : "No data";
            rows[series + 1] = new($"{item.Name}: {value}", this._seriesBrushes[series]);
        }

        return new ChartInspectionItem(this.Position(index, this._radius), rows);
    }

    private ChartContextValue[] ReadContext(int index)
    {
        var data = this._owner.Data!;
        return data.Series.Select((series, seriesIndex) => new ChartContextValue(
            series.Name, series.Values[index], series, seriesIndex, index, Category: data.Axes[index].Name)).ToArray();
    }

    private void Highlight(int index)
    {
        this._selection.Visibility = index >= 0 && index < this._spokes.Length ? Visibility.Visible : Visibility.Collapsed;
        if (this._selection.Visibility == Visibility.Visible)
        {
            var end = this.Position(index, this._radius);
            this._selection.X1 = this._center.X;
            this._selection.Y1 = this._center.Y;
            this._selection.X2 = end.X;
            this._selection.Y2 = end.Y;
        }
    }

    private Point Position(int axis, double radius)
    {
        return new(this._center.X + this._spokes[axis].X * radius, this._center.Y + this._spokes[axis].Y * radius);
    }

    private static PathFigure Segment(Point start, Point end)
    {
        return new() { StartPoint = start, IsFilled = false, Segments = [new LineSegment { Point = end }] };
    }

    private static PathFigure Polygon(Point[] points, bool filled)
    {
        var figure = new PathFigure { StartPoint = points[0], IsClosed = true, IsFilled = filled };
        for (var index = 1; index < points.Length; index++)
        {
            figure.Segments.Add(new LineSegment { Point = points[index] });
        }

        return figure;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs args)
    {
        this.Render();
    }
}
