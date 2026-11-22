// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class ScatterChartPresenter
{
    private readonly ScatterChart _owner;
    private readonly Canvas _plot;
    private readonly CartesianPlotPresenter _cartesian;
    private readonly ChartInspectorPresenter _inspector;
    private readonly AccessibilitySettings _accessibility = new();
    private readonly UISettings _settings = new();
    private readonly List<PlottedPoint> _points = [];
    private readonly Ellipse _selectionOuter = new() { IsHitTestVisible = false, StrokeThickness = 4 };
    private readonly Ellipse _selectionInner = new() { IsHitTestVisible = false, StrokeThickness = 1.5 };
    private SolidColorBrush[] _seriesBrushes = [];
    private double _radius;

    public ScatterChartPresenter(ScatterChart owner, Canvas plot, Grid? legend, Popup? popup, TextBlock? details)
    {
        this._owner = owner;
        this._plot = plot;
        this._cartesian = new CartesianPlotPresenter(owner, plot, legend, ChartLegendSwatch.Dot);
        this._inspector = new ChartInspectorPresenter(
            owner, plot, popup, () => this._points.Count, this.HitTest, this.ReadPoint, this.Highlight, details,
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

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        this.Render();
    }

    public void Render()
    {
        this._plot.Children.Clear();
        this._cartesian.ClearSeriesVisuals();
        this._points.Clear();
        var data = this._owner.Data;
        if (data is null || data.Series.Count == 0)
        {
            this._seriesBrushes = [];
            this._cartesian.RebuildLegend(null, [], this._owner.ShowLegend);
            this.ClearInspection();
            return;
        }

        var highContrast = this._accessibility.HighContrast;
        var palette = ChartPalette.GetEffectiveColors(this._owner);
        var theme = this._owner.ActualTheme;
        var brushes = this._seriesBrushes = new SolidColorBrush[data.Series.Count];
        var names = new string[data.Series.Count];
        var xMin = double.PositiveInfinity;
        var xMax = double.NegativeInfinity;
        var yMin = double.PositiveInfinity;
        var yMax = double.NegativeInfinity;
        for (var index = 0; index < data.Series.Count; index++)
        {
            var series = data.Series[index];
            names[index] = series.Name;
            var color = highContrast
                ? this._settings.UIElementColor(index % 2 == 0 ? UIElementType.WindowText : UIElementType.Highlight)
                : series.MarkerColor ?? palette.GetColor(index, theme);
            brushes[index] = new SolidColorBrush(color);
            foreach (var point in series.Points)
            {
                xMin = Math.Min(xMin, point.X);
                xMax = Math.Max(xMax, point.X);
                yMin = Math.Min(yMin, point.Y);
                yMax = Math.Max(yMax, point.Y);
            }
        }

        this._cartesian.RebuildLegend(
            names, brushes, this._owner.ShowLegend,
            ChartLegend.GetValueMode(this._owner) == ChartLegendValueMode.Value ? ReadLegendValue : null);
        var width = this._plot.ActualWidth;
        var height = this._plot.ActualHeight;
        if (!double.IsFinite(xMin) || width < 20 || height < 20)
        {
            this.ClearInspection();
            return;
        }

        this._plot.Clip = new RectangleGeometry { Rect = new Rect(0, 0, width, height) };
        var xOptions = this._owner.XAxis;
        var yOptions = this._owner.YAxis;
        var xScale = ChartScale.ForAxis(xMin, xMax, (int)Math.Clamp(width / 88, 2, 7), xOptions);
        var yScale = ChartScale.ForAxis(yMin, yMax, (int)Math.Clamp(height / 38, 2, 6), yOptions);
        if (this._cartesian.DrawAxes(
                xScale, yScale, x => ChartInspectorPresenter.FormatValue(x, xOptions?.TickLabelFormat ?? this._owner.XValueFormat),
                y => ChartInspectorPresenter.FormatValue(y, yOptions?.TickLabelFormat ?? this._owner.YValueFormat), this._owner.ShowGrid,
                xOptions, yOptions) is not { } layout)
        {
            this.ClearInspection();
            return;
        }

        var size = this._owner.MarkerSize;
        this._radius = double.IsFinite(size) ? Math.Clamp(size, 1, 48) / 2 : 4;
        for (var index = 0; index < data.Series.Count; index++)
        {
            var series = data.Series[index];
            if (series.Points.Count == 0)
            {
                continue;
            }

            var geometry = new GeometryGroup { FillRule = FillRule.Nonzero };
            foreach (var point in series.Points)
            {
                if (!xScale.Contains(point.X) || !yScale.Contains(point.Y))
                {
                    continue;
                }

                var center = new Point(layout.MapX(point.X), layout.MapY(point.Y));
                geometry.Children.Add(new EllipseGeometry { Center = center, RadiusX = this._radius, RadiusY = this._radius });
                this._points.Add(new PlottedPoint(center, point, series.Name, index, this._points.Count));
            }

            var path = new Path
            {
                Data = geometry,
                Fill = brushes[index],
                Stretch = Stretch.None,
                IsHitTestVisible = false,
                HighContrastAdjustment = highContrast ? ElementHighContrastAdjustment.None : ElementHighContrastAdjustment.Application,
            };
            this._plot.Children.Add(path);
            this._cartesian.TrackSeriesVisual(path, index);
        }

        this._cartesian.DrawReferenceLines(layout, xOptions, yOptions, highContrast);
        ChartMilestoneRenderer.Draw(this._plot, this._owner, layout, this._owner.Milestones, highContrast);

        this._points.Sort(static (left, right) =>
        {
            var x = left.Source.X.CompareTo(right.Source.X);
            if (x != 0)
            {
                return x;
            }

            var y = left.Source.Y.CompareTo(right.Source.Y);
            return y != 0 ? y : left.DrawOrder.CompareTo(right.DrawOrder);
        });

        this._selectionOuter.Stroke
            = highContrast ? new SolidColorBrush(this._settings.UIElementColor(UIElementType.Window)) : this._owner.Background;
        this._selectionInner.Stroke = highContrast
            ? new SolidColorBrush(this._settings.UIElementColor(UIElementType.WindowText))
            : this._owner.Foreground;
        this.Highlight(-1);
        this._plot.Children.Add(this._selectionOuter);
        this._plot.Children.Add(this._selectionInner);
        this._inspector.Refresh();

        string? ReadLegendValue(int index)
        {
            var points = data.Series[index].Points;
            if (points.Count == 0)
            {
                return null;
            }

            var point = points[^1];
            return
                WinUIResourceStrings.FormatDefault("ScatterPointFormat", ChartInspectorPresenter.FormatValue(point.X, this._owner.XValueFormat),
                    ChartInspectorPresenter.FormatValue(point.Y, this._owner.YValueFormat));
        }
    }

    private int HitTest(Point pointer)
    {
        var reach = this._radius + 5;
        var low = 0;
        var high = this._points.Count;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (this._points[middle].Center.X < pointer.X - reach)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        var selected = -1;
        var nearest = reach * reach;
        for (var index = low; index < this._points.Count && this._points[index].Center.X <= pointer.X + reach; index++)
        {
            var point = this._points[index];
            var dx = point.Center.X - pointer.X;
            var dy = point.Center.Y - pointer.Y;
            var distance = dx * dx + dy * dy;
            if (distance < nearest || (distance == nearest && (selected < 0 || point.DrawOrder > this._points[selected].DrawOrder)))
            {
                selected = index;
                nearest = distance;
            }
        }

        return selected;
    }

    private ChartInspectionItem? ReadPoint(int index)
    {
        var point = this._points[index];
        var title = string.IsNullOrWhiteSpace(point.Source.Label) ? point.Series : WinUIResourceStrings.FormatDefault("ScatterPointTitleFormat", point.Series, point.Source.Label);
        return new ChartInspectionItem(
            point.Center, (ChartInspectionRow[])
            [
                new(title, this._seriesBrushes[point.SeriesIndex]), new(WinUIResourceStrings.FormatDefault("ScatterXReadout", this.FormatX(point.Source.X))),
                new(WinUIResourceStrings.FormatDefault("ScatterYReadout", this.FormatY(point.Source.Y)))
            ]);
    }

    private ChartContextValue[] ReadContext(int index)
    {
        var point = this._points[index];
        return [new ChartContextValue(point.Series, point.Source.Y, point.Source, point.SeriesIndex, X: point.Source.X)];
    }

    private void Highlight(int index)
    {
        var visibility = index >= 0 ? Visibility.Visible : Visibility.Collapsed;
        this._selectionOuter.Visibility = this._selectionInner.Visibility = visibility;
        if (index < 0)
        {
            return;
        }

        var center = this._points[index].Center;
        var radius = this._radius + 3;
        Position(this._selectionOuter);
        Position(this._selectionInner);

        void Position(Ellipse ellipse)
        {
            ellipse.Width = ellipse.Height = 2 * radius;
            Canvas.SetLeft(ellipse, center.X - radius);
            Canvas.SetTop(ellipse, center.Y - radius);
        }
    }

    private string FormatX(double value)
    {
        return ChartInspectorPresenter.FormatValue(value, this._owner.XValueFormat);
    }

    private string FormatY(double value)
    {
        return ChartInspectorPresenter.FormatValue(value, this._owner.YValueFormat);
    }

    private readonly record struct PlottedPoint(Point Center, ScatterChartPoint Source, string Series, int SeriesIndex, int DrawOrder);
}
