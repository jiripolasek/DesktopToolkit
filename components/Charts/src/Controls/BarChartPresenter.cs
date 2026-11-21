// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class BarChartPresenter
{
    private readonly BarChart _owner;
    private readonly Canvas _plot;
    private readonly ChartLegendHost? _legend;
    private readonly ChartInspectorPresenter _inspector;
    private readonly ChartPixelSnap _pixels;
    private readonly Canvas _selection = new() { IsHitTestVisible = false };
    private readonly List<BarMark> _marks = [];
    private readonly AccessibilitySettings _accessibility = new();
    private readonly UISettings _uiSettings = new();
    private int _highlightedLegendSeries = -1;
    private int[][]? _groups;
    private int[] _groupByMark = [];
    private int _hoveredMark = -1;
    private Brush? _outline;

    public BarChartPresenter(BarChart owner, Canvas plot, Grid? legend, Popup? popup, TextBlock? details)
    {
        this._owner = owner;
        this._plot = plot;
        this._legend = legend is null ? null : new ChartLegendHost(owner, legend);
        this._inspector = new ChartInspectorPresenter(
            owner, plot, popup, () => this._groups?.Length ?? this._marks.Count, this.HitTest, this.ReadSelection, this.HighlightSelection,
            details, readContext: this.ReadContext);
        this._pixels = new ChartPixelSnap(plot, this.Render);
        this._plot.SizeChanged += this.OnPlotSizeChanged;
    }

    public void Detach()
    {
        this._inspector.Detach();
        this._pixels.Detach();
        this._plot.SizeChanged -= this.OnPlotSizeChanged;
    }

    private void OnPlotSizeChanged(object sender, SizeChangedEventArgs e)
    {
        this.Render();
    }

    public void Render()
    {
        var selectedIndex = this._inspector.FocusedIndex;
        BarIdentity? selected = selectedIndex >= 0 ? this.GetInspectionIdentity(selectedIndex) : null;
        var previous = this._marks.ToDictionary(mark => mark.Identity, mark => mark.Visual);
        this._plot.Children.Clear();
        this._selection.Children.Clear();
        this._marks.Clear();
        this._groups = null;

        var data = this._owner.Data;
        if (data is null || data.Categories.Count == 0 || data.Series.Count == 0)
        {
            this.RebuildLegend(null, []);
            this.ClearInspection();
            return;
        }

        var highContrast = this._accessibility.HighContrast;
        var palette = ChartPalette.GetEffectiveColors(this._owner);
        var theme = this._owner.ActualTheme;
        var brushes = new Brush[data.Series.Count];
        for (var index = 0; index < brushes.Length; index++)
        {
            brushes[index] = highContrast
                ? new SolidColorBrush(this._uiSettings.UIElementColor(index % 2 == 0 ? UIElementType.Highlight : UIElementType.WindowText))
                : data.Series[index].Brush ?? new SolidColorBrush(palette.GetColor(index, theme));
        }

        this._outline = highContrast
            ? new SolidColorBrush(this._uiSettings.UIElementColor(UIElementType.WindowText))
            : this._owner.Foreground;
        this.RebuildLegend(data, brushes);

        var width = this._plot.ActualWidth;
        var height = this._plot.ActualHeight;
        if (width <= 2 || height <= 2)
        {
            this.ClearInspection();
            return;
        }

        this._plot.Clip = new RectangleGeometry { Rect = new Rect(0, 0, width, height) };
        var horizontal = this._owner.Orientation == Orientation.Horizontal;
        var mode = this._owner.Mode;
        var percent = mode == BarChartMode.Stacked100;
        var segments = BarChartSegments.Create(data, mode);
        var axisLength = horizontal ? width : height;
        var valueAxis = this._owner.ValueAxis;
        var categoryAxis = this._owner.CategoryAxis;
        var scale = percent
            ? ChartScale.Percent
            : ChartScale.ForBars(
                segments.Minimum, segments.Maximum, valueAxis?.Minimum ?? this._owner.Minimum, valueAxis?.Maximum ?? this._owner.Maximum,
                (int)Math.Clamp(axisLength / 52, 2, 6));
        var presentation = ChartAxisAppearance.GetPresentation(this._owner);
        var showLabels = presentation != ChartAxisPresentation.Hidden;
        var showLines = presentation == ChartAxisPresentation.Standard;
        var showGrid = showLines && (valueAxis?.ShowGridLines ?? this._owner.ShowGrid);
        var showBaseline = showLines && valueAxis?.ShowAxisLine != false && scale.Ticks.Contains(0d);
        var tickLabels = !showLabels || valueAxis?.ShowLabels == false
            ? []
            : scale.Ticks.Select(value => this.MeasureLabel(
                    percent
                        ? value.ToString("P0", CultureInfo.CurrentCulture)
                        : ChartInspectorPresenter.FormatValue(value, valueAxis?.TickLabelFormat ?? this._owner.ValueFormat, "G4"),
                    valueAxis))
                .ToArray();
        var categoryLabels = !showLabels || categoryAxis?.ShowLabels == false
            ? []
            : data.Categories.Select(label => this.MeasureLabel(label, categoryAxis)).ToArray();
        var tickHeight = tickLabels.Select(label => label.DesiredSize.Height).DefaultIfEmpty().Max();
        var textHeight = Math.Max(tickHeight, categoryLabels.Select(label => label.DesiredSize.Height).DefaultIfEmpty().Max());
        var tickWidth = Math.Min(tickLabels.Select(label => label.DesiredSize.Width).DefaultIfEmpty().Max(), width * 0.24);
        var categoryWidth = Math.Min(categoryLabels.Select(label => label.DesiredSize.Width).DefaultIfEmpty().Max(), width * 0.32);
        var fullPlot = tickLabels.Length == 0 && categoryLabels.Length == 0 && !showBaseline && !showGrid;
        var left = (horizontal ? Math.Max(categoryWidth, tickWidth / 2) : tickWidth)
                   + (tickLabels.Length > 0 || (horizontal && categoryLabels.Length > 0) ? 8 : 2);
        var right = width - (horizontal ? tickWidth / 2 + 2 : 4);
        var top = horizontal ? 4 : (tickLabels.Length > 0 ? textHeight / 2 : 0) + 2;
        var hasBottomLabels = horizontal ? tickLabels.Length > 0 : categoryLabels.Length > 0;
        var bottom = height - (hasBottomLabels ? textHeight + 8 : (horizontal ? 0 : tickHeight / 2) + 2);
        if (right - left < 1 || bottom - top < 1)
        {
            this.ClearInspection();
            return;
        }

        var bounds = fullPlot ? new Rect(0, 0, width, height) : new Rect(left, top, right - left, bottom - top);
        var layout = PlotLayout.Create(horizontal, bounds, data.Categories.Count, brushes.Length, mode, this._owner.BarSpacing);
        this.DrawValueAxis(layout, scale, tickLabels, tickWidth, textHeight, showGrid, showBaseline);
        if (categoryLabels.Length > 0)
        {
            this.DrawCategoryLabels(layout, categoryLabels, categoryWidth, textHeight);
        }

        this.DrawBars(data, segments.Items, layout, scale, brushes, highContrast, showBaseline, previous);
        ChartReferenceLineRenderer.Draw(
            this._plot, bounds, scale, valueAxis?.ReferenceLines, horizontal, highContrast ? this._outline : this._owner.Foreground,
            this._owner.FontFamily, highContrast);
        this._plot.Children.Add(this._selection);
        this.BuildInspectionGroups();
        var restored = -1;
        if (selected is BarIdentity identity)
        {
            var count = this._groups?.Length ?? this._marks.Count;
            for (var index = 0; index < count; index++)
            {
                if (this.GetInspectionIdentity(index) == identity)
                {
                    restored = index;
                    break;
                }
            }
        }

        this._inspector.Refresh(restored);
    }

    private BarIdentity GetInspectionIdentity(int index)
    {
        var mark = this._marks[this._groups is null ? index : this._groups[index][0]];
        return this._owner.InspectionMode switch
        {
            BarChartInspectionMode.Category => mark.Identity with { Series = default },
            BarChartInspectionMode.Series => mark.Identity with { Category = default },
            _ => mark.Identity,
        };
    }

    public void ClearInspection()
    {
        this._inspector.Clear();
    }

    public void UpdateInspectionMode()
    {
        this.ClearInspection();
        this.BuildInspectionGroups();
    }

    private void BuildInspectionGroups()
    {
        this._hoveredMark = -1;
        var mode = this._owner.InspectionMode;
        if (mode is not (BarChartInspectionMode.Category or BarChartInspectionMode.Series))
        {
            this._groups = null;
            this._groupByMark = [];
            return;
        }

        this._groups = Enumerable.Range(0, this._marks.Count)
            .GroupBy(index => mode == BarChartInspectionMode.Category ? this._marks[index].Category : this._marks[index].Series)
            .OrderBy(group => group.Key).Select(group => group.ToArray()).ToArray();
        this._groupByMark = new int[this._marks.Count];
        for (var group = 0; group < this._groups.Length; group++)
        {
            foreach (var mark in this._groups[group])
            {
                this._groupByMark[mark] = group;
            }
        }
    }

    private int HitTest(Point point)
    {
        this._hoveredMark = this._marks.FindIndex(mark => mark.Bounds.Contains(point));
        return this._hoveredMark >= 0 && this._groups is not null ? this._groupByMark[this._hoveredMark] : this._hoveredMark;
    }

    private ChartInspectionItem? ReadSelection(int index)
    {
        if (this._groups is null)
        {
            var mark = this._marks[index];
            return new ChartInspectionItem(
                GetAnchor(mark), (ChartInspectionRow[])[new(mark.Description, mark.Visual.Background, mark.Visual.BorderBrush)]);
        }

        var data = this._owner.Data!;
        var group = this._groups[index];
        var first = this._marks[group[0]];
        var byCategory = this._owner.InspectionMode == BarChartInspectionMode.Category;
        var rows = new ChartInspectionRow[group.Length + 1];
        rows[0] = byCategory
            ? new(data.Categories[first.Category])
            : new(data.Series[first.Series].Name, first.Visual.Background, first.Visual.BorderBrush);
        for (var row = 0; row < group.Length; row++)
        {
            var mark = this._marks[group[row]];
            var label = byCategory ? data.Series[mark.Series].Name : data.Categories[mark.Category];
            rows[row + 1] = new(
                $"{label}: {mark.ValueText}", byCategory ? mark.Visual.Background : null, byCategory ? mark.Visual.BorderBrush : null);
        }

        var anchor = this._hoveredMark >= 0 && this._groupByMark[this._hoveredMark] == index ? this._marks[this._hoveredMark] : first;
        return new ChartInspectionItem(GetAnchor(anchor), rows);
    }

    private void HighlightSelection(int index)
    {
        this._selection.Children.Clear();

        if (index < 0)
        {
            return;
        }

        if (this._groups is null)
        {
            this.Highlight(this._marks[index]);
            return;
        }

        foreach (var mark in this._groups[index])
        {
            this.Highlight(this._marks[mark]);
        }
    }

    private ChartContextValue[] ReadContext(int index)
    {
        var data = this._owner.Data!;
        return this._groups is null ? [ReadMark(index)] : this._groups[index].Select(ReadMark).ToArray();

        ChartContextValue ReadMark(int markIndex)
        {
            var mark = this._marks[markIndex];
            var series = data.Series[mark.Series];
            return new ChartContextValue(
                series.Name, series.Values[mark.Category], series, mark.Series, mark.Category, Category: data.Categories[mark.Category]);
        }
    }

    private void Highlight(BarMark mark)
    {
        var bounds = mark.Bounds;
        var backdrop = this._accessibility.HighContrast
            ? new SolidColorBrush(this._uiSettings.UIElementColor(UIElementType.Window))
            : this._owner.Background;
        AddStroke(backdrop, 4);
        AddStroke(this._outline, 1.5);

        void AddStroke(Brush? brush, double thickness)
        {
            var outline = new Border
            {
                Width = bounds.Width,
                Height = bounds.Height,
                CornerRadius = mark.Visual.CornerRadius,
                BorderBrush = brush,
                BorderThickness = new Thickness(Math.Min(thickness, Math.Min(bounds.Width, bounds.Height))),
                HighContrastAdjustment = ElementHighContrastAdjustment.None,
            };
            Canvas.SetLeft(outline, bounds.Left);
            Canvas.SetTop(outline, bounds.Top);
            this._selection.Children.Add(outline);
        }
    }

    private static Point GetAnchor(BarMark mark)
    {
        return new(mark.Bounds.Right, mark.Bounds.Top + mark.Bounds.Height / 2);
    }

    private void DrawValueAxis(
        PlotLayout layout,
        ChartScale scale,
        TextBlock[] labels,
        double labelWidth,
        double textHeight,
        bool showGrid,
        bool showBaseline)
    {
        var bounds = layout.Bounds;
        var lastLabelEnd = double.NegativeInfinity;
        var baselineBrush = this._accessibility.HighContrast
            ? this._outline
            : ChartAxisAppearance.GetLineBrush(this._owner) ?? this._outline;
        var gridBrush = ChartAxisAppearance.GetGridLineBrush(this._owner) ?? this._owner.BorderBrush;
        for (var index = 0; index < scale.Ticks.Count; index++)
        {
            var tick = scale.Ticks[index];
            var value = layout.ValuePosition(scale.Normalize(tick));
            var baseline = tick == 0;
            if (baseline ? showBaseline : showGrid)
            {
                this._plot.Children.Add(
                    new Line
                    {
                        X1 = layout.Horizontal ? value : bounds.Left,
                        X2 = layout.Horizontal ? value : bounds.Right,
                        Y1 = layout.Horizontal ? bounds.Top : value,
                        Y2 = layout.Horizontal ? bounds.Bottom : value,
                        Stroke = baseline ? baselineBrush : gridBrush,
                        Opacity = baseline ? 1 : 0.85,
                        IsHitTestVisible = false,
                    });
            }

            if (labels.Length == 0)
            {
                continue;
            }

            var label = labels[index];
            if (layout.Horizontal)
            {
                var start = value - labelWidth / 2;
                if (start >= lastLabelEnd)
                {
                    this.PlaceLabel(label, start, bounds.Bottom + 5, labelWidth, TextAlignment.Center);
                    lastLabelEnd = start + labelWidth + 4;
                }
            }
            else if (index == 0 || lastLabelEnd - value >= textHeight + 2)
            {
                this.PlaceLabel(label, 0, value - textHeight / 2, labelWidth, TextAlignment.Right);
                lastLabelEnd = value;
            }
        }
    }

    private void DrawCategoryLabels(PlotLayout layout, TextBlock[] labels, double horizontalWidth, double textHeight)
    {
        var bounds = layout.Bounds;
        var slot = layout.CategoryPitch;
        var labelSize = layout.Horizontal ? textHeight : Math.Min(bounds.Width, labels.Max(label => label.DesiredSize.Width));
        var stride = Math.Max(1, (int)Math.Ceiling((labelSize + 4) / slot));
        for (var category = 0; category < labels.Length; category += stride)
        {
            var center = layout.CategoryCenter(category);
            if (layout.Horizontal)
            {
                this.PlaceLabel(labels[category], 0, bounds.Top + center - textHeight / 2, horizontalWidth, TextAlignment.Right);
            }
            else
            {
                var width = Math.Min(bounds.Width, slot * stride - 4);
                var x = Math.Clamp(bounds.Left + center - width / 2, bounds.Left, bounds.Right - width);
                this.PlaceLabel(labels[category], x, bounds.Bottom + 5, width, TextAlignment.Center);
            }
        }
    }

    private void DrawBars(
        BarChartData data,
        IReadOnlyList<BarChartSegment> segments,
        PlotLayout layout,
        ChartScale scale,
        Brush[] brushes,
        bool highContrast,
        bool showBaseline,
        Dictionary<BarIdentity, Border> previous)
    {
        var stacked = this._owner.Mode is BarChartMode.Stacked or BarChartMode.Stacked100;
        var barsPerCategory = stacked ? 1 : brushes.Length;
        var thickness = layout.BarThickness;
        var pixelScale = this._pixels.Scale;
        var pixelSize = pixelScale > 0 ? 1 / pixelScale : 1;
        var gap = this._owner.SegmentGap;
        var halfGap = stacked && double.IsFinite(gap) && gap > 0 ? gap / 2 : 0;
        var cornerRadius = this._owner.SegmentCornerRadius;
        if (!double.IsFinite(cornerRadius) || cornerRadius < 0)
        {
            cornerRadius = 0;
        }

        double? baseline = showBaseline ? ValuePosition(0) : null;
        var outerOnly = stacked && this._owner.SegmentRounding != BarChartSegmentRounding.AllCorners;
        var stackExtents = halfGap > 0 || (outerOnly && cornerRadius > 0)
            ? GetStackExtents(segments, layout.CategoryCount, ValuePosition)
            : null;
        foreach (var segment in segments)
        {
            var slot = stacked ? 0 : segment.Series;
            var offset = layout.CategoryCenter(segment.Category) + (slot - (barsPerCategory - 1) / 2d) * layout.BarPitch - thickness / 2;
            var first = ValuePosition(segment.Start);
            var last = ValuePosition(segment.End);
            var start = Math.Min(first, last);
            var end = Math.Max(first, last);
            var length = end - start;
            if (stacked && length == 0)
            {
                continue;
            }

            if (!stacked && length < 1)
            {
                var lowerEdge = layout.Horizontal ? layout.Bounds.Left : layout.Bounds.Top;
                var upperEdge = layout.Horizontal ? layout.Bounds.Right : layout.Bounds.Bottom;
                start = Math.Clamp(start - 0.5, lowerEdge, upperEdge - 1);
                length = 1;
                end = start + length;
            }

            var (stackStart, stackEnd) = stackExtents?[segment.Category] ?? (start, end);
            var startsStack = start == stackStart;
            var endsStack = end == stackEnd;
            if (halfGap > 0)
            {
                var inset = Math.Min(halfGap, Math.Min(length / 4, Math.Max(0, length - pixelSize) / 2));
                start += startsStack ? 0 : inset;
                end -= endsStack ? 0 : inset;
                length = end - start;
            }

            var series = data.Series[segment.Series];
            var identity = new BarIdentity(
                ItemIdentity.Create(data, data.CategoryKeys?[segment.Category], segment.Category),
                ItemIdentity.Create(data, series.Key, segment.Series));
            var bar = previous.GetValueOrDefault(identity) ?? new Border();
            this.UpdateBarBrushes(bar, brushes[segment.Series], highContrast, segment.Series);
            bar.Opacity = ChartLegendHighlight.OpacityFor(segment.Series, this._highlightedLegendSeries);
            var radius = stacked ? Math.Min(cornerRadius, Math.Min(length, thickness) / 2) : 2;
            var startRadius = start != baseline && (!outerOnly || startsStack) ? radius : 0;
            var endRadius = end != baseline && (!outerOnly || endsStack) ? radius : 0;
            bar.CornerRadius = layout.Horizontal
                ? new CornerRadius(startRadius, endRadius, endRadius, startRadius)
                : new CornerRadius(startRadius, startRadius, endRadius, endRadius);

            bar.Width = layout.Horizontal ? length : thickness;
            bar.Height = layout.Horizontal ? thickness : length;
            var x = layout.Horizontal ? start : layout.Bounds.Left + offset;
            var y = layout.Horizontal ? layout.Bounds.Top + offset : start;
            Canvas.SetLeft(bar, x);
            Canvas.SetTop(bar, y);
            var valueText = this.FormatValue(segment.Value);
            if (segment.Share is double share)
            {
                valueText += $" ({share.ToString("P1", CultureInfo.CurrentCulture)})";
            }

            var description = $"{data.Categories[segment.Category]}, {series.Name}: {valueText}";
            AutomationProperties.SetName(bar, description);
            this._plot.Children.Add(bar);
            this._marks.Add(
                new BarMark(
                    identity, bar, new Rect(x, y, bar.Width, bar.Height), segment.Category, segment.Series, valueText, description));
        }

        double ValuePosition(double value)
        {
            var position = layout.ValuePosition(scale.Normalize(value));
            return stacked && pixelScale > 0 ? ChartPixelSnap.Round(position, pixelScale) : position;
        }
    }

    private static (double Start, double End)[] GetStackExtents(
        IReadOnlyList<BarChartSegment> segments,
        int categoryCount,
        Func<double, double> valuePosition)
    {
        var zero = valuePosition(0);
        var extents = new (double Start, double End)[categoryCount];
        Array.Fill(extents, (zero, zero));
        foreach (var segment in segments)
        {
            var end = valuePosition(segment.End);
            ref var extent = ref extents[segment.Category];
            extent = (Math.Min(extent.Start, end), Math.Max(extent.End, end));
        }

        return extents;
    }

    private void RebuildLegend(BarChartData? data, Brush[] brushes)
    {
        var highContrast = this._accessibility.HighContrast;
        var showValues = ChartLegend.GetValueMode(this._owner) == ChartLegendValueMode.Value;
        var entries = new ChartLegendEntry[brushes.Length];
        for (var index = 0; index < entries.Length; index++)
        {
            var series = data!.Series[index];
            var outlined = highContrast && index % 4 >= 2;
            var fill = outlined ? new SolidColorBrush(this._uiSettings.UIElementColor(UIElementType.Window)) : brushes[index];
            var text = showValues && series.Values.LastOrDefault(static value => value.HasValue) is double value
                ? this.FormatValue(value)
                : null;
            entries[index] = new(series.Name, fill, text, outlined ? brushes[index] : null);
        }

        ChartLegendSource.For(this._owner).Update(ChartLegendSwatch.Box, entries, this.HighlightLegendSeries);
        this._legend?.Update(this._owner.ShowLegend);
    }

    private void HighlightLegendSeries(int index)
    {
        this._highlightedLegendSeries = index;
        foreach (var mark in this._marks)
        {
            mark.Visual.Opacity = ChartLegendHighlight.OpacityFor(mark.Series, index);
        }
    }

    private void UpdateBarBrushes(Border bar, Brush brush, bool highContrast, int index)
    {
        var outlined = highContrast && index % 4 >= 2;
        bar.HighContrastAdjustment = highContrast ? ElementHighContrastAdjustment.None : ElementHighContrastAdjustment.Application;
        bar.Background = outlined ? new SolidColorBrush(this._uiSettings.UIElementColor(UIElementType.Window)) : brush;
        bar.BorderBrush = outlined ? brush : null;
        bar.BorderThickness = new Thickness(outlined ? 1 : 0);
    }

    private TextBlock OpenLabel(string text)
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
        var label = this.OpenLabel(text);
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

    private string FormatValue(double value)
    {
        try
        {
            return value.ToString(this._owner.ValueFormat ?? "G4", CultureInfo.CurrentCulture);
        }
        catch (FormatException)
        {
            return value.ToString("G4", CultureInfo.CurrentCulture);
        }
    }

    private readonly record struct ItemIdentity(BarChartData? Snapshot, string? Key, int Index)
    {
        public static ItemIdentity Create(BarChartData data, string? key, int index)
        {
            return key is null ? new(data, null, index) : new(null, key, 0);
        }
    }

    private readonly record struct BarIdentity(ItemIdentity Category, ItemIdentity Series);

    private sealed record BarMark(
        BarIdentity Identity,
        Border Visual,
        Rect Bounds,
        int Category,
        int Series,
        string ValueText,
        string Description);

    private readonly record struct PlotLayout(
        bool Horizontal,
        Rect Bounds,
        int CategoryCount,
        double CategoryPitch,
        double CategoryOffset,
        double BarPitch,
        double BarThickness)
    {
        public static PlotLayout Create(
            bool horizontal,
            Rect bounds,
            int categoryCount,
            int seriesCount,
            BarChartMode mode,
            double barSpacing)
        {
            var stacked = mode is BarChartMode.Stacked or BarChartMode.Stacked100;
            var span = horizontal ? bounds.Height : bounds.Width;
            var slot = span / categoryCount;
            var barsPerCategory = stacked ? 1 : seriesCount;
            if (!double.IsFinite(barSpacing) || barSpacing < 0)
            {
                var groupWidth = Math.Min(slot * 0.78, stacked ? 64 : 40 * (double)seriesCount);
                var pitch = groupWidth / barsPerCategory;
                return new(horizontal, bounds, categoryCount, slot, 0, pitch, pitch * 0.84);
            }

            var barCount = (double)categoryCount * barsPerCategory;
            var gap = Math.Min(barSpacing, Math.Max(0, (span - barCount) / Math.Max(1, barCount - 1)));
            var barPitch = (span + gap) / barCount;
            return new(horizontal, bounds, categoryCount, barPitch * barsPerCategory, gap / 2, barPitch, barPitch - gap);
        }

        public double CategoryCenter(int category)
        {
            return (category + 0.5) * this.CategoryPitch - this.CategoryOffset;
        }

        public double ValuePosition(double normalized)
        {
            return this.Horizontal
                ? this.Bounds.Left + normalized * this.Bounds.Width
                : this.Bounds.Bottom - normalized * this.Bounds.Height;
        }
    }
}
