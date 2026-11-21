// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class CartesianPlotPresenter(Control owner, Canvas plot, Grid? legend, ChartLegendSwatch swatch = ChartLegendSwatch.Line)
{
    private readonly Control _owner = owner;
    private readonly Canvas _plot = plot;
    private readonly Grid? _legend = legend;
    private readonly ChartLegendHighlight _highlight = new();
    private ChartLegendHost? _legendHost;

    public void ClearSeriesVisuals()
    {
        this._highlight.ClearMarks();
    }

    public void TrackSeriesVisual(UIElement visual, int index)
    {
        this._highlight.Add(visual, index);
    }

    public CartesianPlotLayout? DrawAxes(
        ChartScale xScale,
        ChartScale yScale,
        Func<double, string> formatX,
        Func<double, string> formatY,
        bool showGrid,
        ChartAxisOptions? xOptions,
        ChartAxisOptions? yOptions)
    {
        var width = this._plot.ActualWidth;
        var height = this._plot.ActualHeight;
        var presentation = ChartAxisAppearance.GetPresentation(this._owner);
        var showLabels = presentation != ChartAxisPresentation.Hidden;
        var showLines = presentation == ChartAxisPresentation.Standard;
        var xLabels = !showLabels || xOptions?.ShowLabels == false
            ? []
            : xScale.Ticks.Select(x => this.MeasureLabel(formatX(x), xOptions)).ToArray();
        var yLabels = !showLabels || yOptions?.ShowLabels == false
            ? []
            : yScale.Ticks.Select(y => this.MeasureLabel(formatY(y), yOptions)).ToArray();
        var showXGrid = showLines && (xOptions?.ShowGridLines ?? showGrid);
        var showYGrid = showLines && (yOptions?.ShowGridLines ?? showGrid);
        var showXAxis = showLines && xOptions?.ShowAxisLine != false;
        var showYAxis = showLines && yOptions?.ShowAxisLine != false;
        if (!showXGrid && !showYGrid && !showXAxis && !showYAxis && xLabels.Length == 0 && yLabels.Length == 0)
        {
            return new CartesianPlotLayout(new Rect(0, 0, width, height), xScale, yScale);
        }

        var labelHeight = Math.Max(LabelHeight(xLabels), LabelHeight(yLabels));
        var xLabelWidth = Math.Min(width * 0.3, LabelWidth(xLabels));
        var yLabelWidth = Math.Min(width * 0.23, LabelWidth(yLabels));
        var left = Math.Max(yLabels.Length > 0 ? yLabelWidth + 8 : 2, xLabelWidth / 2 + 2);
        var right = width - xLabelWidth / 2 - 2;
        var top = labelHeight / 2 + 2;
        var bottom = height - (xLabels.Length > 0 ? labelHeight + 8 : 2);
        if (right <= left || bottom <= top)
        {
            return null;
        }

        var layout = new CartesianPlotLayout(new Rect(left, top, right - left, bottom - top), xScale, yScale);
        this.DrawAxes(layout, xLabels, yLabels, xLabelWidth, yLabelWidth, labelHeight, showXGrid, showYGrid, showXAxis, showYAxis);
        return layout;
    }

    private static double LabelHeight(TextBlock[] labels)
    {
        return labels.Length == 0 ? 0 : labels.Max(label => label.DesiredSize.Height);
    }

    private static double LabelWidth(TextBlock[] labels)
    {
        return labels.Length == 0 ? 0 : labels.Max(GetTickLabelWidth);
    }

    private static double GetTickLabelWidth(TextBlock label)
    {
        return Math.Ceiling(label.DesiredSize.Width) + 1;
    }

    private void DrawAxes(
        CartesianPlotLayout layout,
        TextBlock[] xLabels,
        TextBlock[] yLabels,
        double xWidth,
        double yWidth,
        double labelHeight,
        bool showXGrid,
        bool showYGrid,
        bool showXAxis,
        bool showYAxis)
    {
        var bounds = layout.Bounds;
        var gridBrush = ChartAxisAppearance.GetGridLineBrush(this._owner) ?? this._owner.BorderBrush;
        if (showXGrid)
        {
            foreach (var x in layout.X.Ticks)
            {
                var coordinate = layout.MapX(x);
                this.AddGuide(coordinate, bounds.Top, coordinate, bounds.Bottom, gridBrush);
            }
        }

        if (showYGrid)
        {
            foreach (var y in layout.Y.Ticks)
            {
                var coordinate = layout.MapY(y);
                this.AddGuide(bounds.Left, coordinate, bounds.Right, coordinate, gridBrush);
            }
        }

        var axisBrush = ChartAxisAppearance.GetLineBrush(this._owner) ?? this._owner.Foreground;
        if (showYAxis)
        {
            this.AddGuide(bounds.Left, bounds.Top, bounds.Left, bounds.Bottom, axisBrush);
        }

        if (showXAxis)
        {
            this.AddGuide(bounds.Left, bounds.Bottom, bounds.Right, bounds.Bottom, axisBrush);
        }

        var lastX = double.NegativeInfinity;
        for (var index = 0; index < xLabels.Length; index++)
        {
            var label = xLabels[index];
            var width = Math.Min(xWidth, GetTickLabelWidth(label));
            var start = layout.MapX(layout.X.Ticks[index]) - width / 2;
            if (start >= lastX)
            {
                this.PlaceLabel(label, start, bounds.Bottom + 5, width, TextAlignment.Center);
                lastX = start + width + 6;
            }
        }

        var lastY = double.PositiveInfinity;
        for (var index = 0; index < yLabels.Length; index++)
        {
            var y = layout.MapY(layout.Y.Ticks[index]);
            if (lastY - y >= labelHeight + 2)
            {
                this.PlaceLabel(yLabels[index], bounds.Left - yWidth - 8, y - labelHeight / 2, yWidth, TextAlignment.Right);
                lastY = y;
            }
        }
    }

    private void AddGuide(double x1, double y1, double x2, double y2, Brush brush)
    {
        this._plot.Children.Add(
            new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = brush,
                Opacity = 0.65,
                IsHitTestVisible = false,
            });
    }

    public void DrawReferenceLines(
        CartesianPlotLayout layout,
        ChartAxisOptions? xOptions,
        ChartAxisOptions? yOptions,
        bool highContrast,
        long? timeOriginTicks = null)
    {
        var foreground = this._owner.Foreground;
        ChartReferenceLineRenderer.Draw(
            this._plot, layout.Bounds, layout.Y, yOptions?.ReferenceLines, false, foreground, this._owner.FontFamily, highContrast);
        ChartReferenceLineRenderer.Draw(
            this._plot, layout.Bounds, layout.X, xOptions?.ReferenceLines, true, foreground, this._owner.FontFamily, highContrast,
            timeOriginTicks);
    }

    public void RebuildLegend(
        string[]? names,
        SolidColorBrush[] brushes,
        bool showLegend,
        Func<int, string?>? value = null,
        Func<int, bool>? isStrokeDashed = null)
    {
        var entries = new ChartLegendEntry[names?.Length ?? 0];
        for (var index = 0; index < entries.Length; index++)
        {
            entries[index] = new(
                names![index], brushes[index], value?.Invoke(index), IsStrokeDashed: isStrokeDashed?.Invoke(index) == true);
        }

        ChartLegendSource.For(this._owner).Update(swatch, entries, this._highlight.Select);
        if (this._legend is not null)
        {
            this._legendHost ??= new ChartLegendHost(this._owner, this._legend);
            this._legendHost.Update(showLegend);
        }
    }

    private TextBlock CreateLabel(string text)
    {
        return new()
        {
            Text = text,
            Foreground = this._owner.Foreground,
            FontFamily = this._owner.FontFamily,
            FontSize = this._owner.FontSize,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1,
        };
    }

    private TextBlock MeasureLabel(string text, ChartAxisOptions? options)
    {
        var label = this.CreateLabel(text);
        label.Foreground = ChartAxisAppearance.GetLabelBrush(this._owner) ?? this._owner.Foreground;
        label.Opacity = options?.LabelOpacity ?? 1;
        label.IsHitTestVisible = false;
        label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return label;
    }

    private void PlaceLabel(TextBlock label, double x, double y, double width, TextAlignment alignment)
    {
        label.Width = width;
        label.TextAlignment = alignment;
        Canvas.SetLeft(label, x);
        Canvas.SetTop(label, y);
        this._plot.Children.Add(label);
    }
}

internal readonly record struct CartesianPlotLayout(Rect Bounds, ChartScale X, ChartScale Y)
{
    public double MapX(double x)
    {
        return this.Bounds.Left + this.X.Project(x) * this.Bounds.Width;
    }

    public double MapY(double y)
    {
        return this.Bounds.Bottom - this.Y.Project(y) * this.Bounds.Height;
    }
}
