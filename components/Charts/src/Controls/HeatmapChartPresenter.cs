// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Globalization;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class HeatmapChartPresenter
{
    private readonly HeatmapChart _owner;
    private readonly Canvas _plot;
    private readonly ChartLegendHost? _legend;
    private readonly ChartInspectorPresenter _inspector;
    private readonly AccessibilitySettings _accessibility = new();
    private readonly UISettings _settings = new();
    private readonly Rectangle _selection = new() { IsHitTestVisible = false, StrokeThickness = 2 };
    private CanvasControl? _surface;
    private HeatmapLayout _layout;
    private int _selected = -1;

    public HeatmapChartPresenter(HeatmapChart owner, Canvas plot, Grid? legend, Popup? popup, TextBlock? details)
    {
        this._owner = owner;
        this._plot = plot;
        this._legend = legend is null ? null : new ChartLegendHost(owner, legend);
        this._inspector = new ChartInspectorPresenter(
            owner, plot, popup, () => owner.Data?.Count ?? 0, this.HitTest, this.ReadCell, this.Highlight, details,
            readContext: this.ReadContext, move: this.Move);
        this._plot.SizeChanged += this.OnSizeChanged;
        this._owner.Loaded += this.OnLoaded;
        this._owner.Unloaded += this.OnUnloaded;
        if (owner.IsLoaded)
        {
            this.CreateSurface();
        }
    }

    public void Detach()
    {
        this._plot.SizeChanged -= this.OnSizeChanged;
        this._owner.Loaded -= this.OnLoaded;
        this._owner.Unloaded -= this.OnUnloaded;
        this._inspector.Detach();
        this.RemoveSurface();
        this._plot.Children.Clear();
    }

    public void ClearInspection()
    {
        this._inspector.Clear();
    }

    public void UpdateAppearance()
    {
        this.RefreshLegend();
        this.Redraw();
    }

    public void Render()
    {
        for (var index = this._plot.Children.Count - 1; index >= 0; index--)
        {
            if (this._plot.Children[index] != this._surface)
            {
                this._plot.Children.RemoveAt(index);
            }
        }

        var width = this._plot.ActualWidth;
        var height = this._plot.ActualHeight;
        if (this._surface is not null)
        {
            this._surface.Width = width;
            this._surface.Height = height;
        }

        this._plot.Clip = new RectangleGeometry { Rect = new Rect(0, 0, width, height) };
        this._layout = default;
        if (this._owner.Data is { Count: > 0 } data && width > 0 && height > 0)
        {
            var showLabels = ChartAxisAppearance.GetPresentation(this._owner) != ChartAxisPresentation.Hidden;
            var rows = showLabels ? this.MeasureLabels(data.RowLabels, this._owner.RowAxis) : [];
            var columns = showLabels ? this.MeasureLabels(data.ColumnLabels, this._owner.ColumnAxis) : [];
            var left = rows.Count == 0 ? 0 : Math.Min(width * 0.25, rows.Max(item => item.Label.DesiredSize.Width) + 7);
            var top = columns.Count == 0 ? 0 : Math.Min(height * 0.3, columns.Max(item => item.Label.DesiredSize.Height) + 4);
            this._layout = HeatmapLayout.Create(data, width, height, left, top, this._owner.CellSpacing, this._owner.KeepCellsSquare);
            if (this._layout.IsValid)
            {
                this.DrawLabels(rows, columns, top, width);
            }
        }

        this._plot.Children.Add(this._selection);
        this.Highlight(this._selected);
        this.Redraw();
    }

    private void Redraw()
    {
        this._surface?.Invalidate();
        this._inspector.Refresh();
    }

    private List<(int Index, TextBlock Label)> MeasureLabels(IReadOnlyList<string?> labels, ChartAxisOptions? axis)
    {
        if (axis?.ShowLabels == false)
        {
            return [];
        }

        var measured = new List<(int, TextBlock)>();
        for (var index = 0; index < labels.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(labels[index]))
            {
                continue;
            }

            var label = new TextBlock
            {
                Text = labels[index]!,
                Foreground = ChartAxisAppearance.GetLabelBrush(this._owner) ?? this._owner.Foreground,
                FontFamily = this._owner.FontFamily,
                FontSize = this._owner.FontSize,
                TextTrimming = TextTrimming.CharacterEllipsis,
                IsHitTestVisible = false,
                Opacity = axis?.LabelOpacity ?? 1,
            };
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            measured.Add((index, label));
        }

        return measured;
    }

    private void DrawLabels(List<(int Index, TextBlock Label)> rows, List<(int Index, TextBlock Label)> columns, double top, double width)
    {
        var next = 0d;
        foreach (var (index, label) in rows)
        {
            var bounds = this._layout.Cell(index, 0);
            var y = bounds.Top + (bounds.Height - label.DesiredSize.Height) / 2;
            if (y >= next && y + label.DesiredSize.Height <= this._plot.ActualHeight)
            {
                var available = Math.Min(this._plot.ActualWidth * 0.25, this._layout.Left - 5);
                var labelWidth = Math.Min(label.DesiredSize.Width, Math.Max(0, available));
                if (labelWidth > 0)
                {
                    this.AddLabel(label, this._layout.Left - 5 - labelWidth, y, labelWidth);
                }

                next = y + label.DesiredSize.Height + 2;
            }
        }

        next = 0;
        foreach (var (index, label) in columns)
        {
            var x = this._layout.Cell(0, index).Left;
            var labelWidth = Math.Min(label.DesiredSize.Width, width - x);
            if (x >= next && labelWidth >= 10)
            {
                this.AddLabel(label, x, this._layout.Top - top, labelWidth);
                next = x + labelWidth + 4;
            }
        }
    }

    private void AddLabel(TextBlock label, double x, double y, double width)
    {
        label.Width = width;
        Canvas.SetLeft(label, x);
        Canvas.SetTop(label, y);
        this._plot.Children.Add(label);
    }

    private void OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        if (!this._layout.IsValid || this._owner.Data is not { } data)
        {
            return;
        }

        var session = args.DrawingSession;
        var (highContrast, scale, low, high) = this.GetColors();
        var radius = this.GetCornerRadius();
        for (var row = 0; row < data.RowCount; row++)
        {
            for (var column = 0; column < data.ColumnCount; column++)
            {
                if (data[row, column] is not { } cell)
                {
                    continue;
                }

                var color = GetCellColor(data, cell, scale, low, high);
                var bounds = this._layout.Cell(row, column);
                session.FillRoundedRectangle(bounds, radius, radius, color);
                if (highContrast && bounds.Width >= 3 && bounds.Height >= 3)
                {
                    session.DrawRoundedRectangle(bounds, radius, radius, high, 0.75f);
                }
            }
        }
    }

    private static Color GetCellColor(HeatmapChartData data, HeatmapChartCell cell, HeatmapColorScale? scale, Color low, Color high)
    {
        return scale?.GetColor(cell.Value)
               ?? HeatmapColorScale.Interpolate(low, high, ChartScale.Normalize(cell.Value, data.Minimum, data.Maximum));
    }

    private (bool HighContrast, HeatmapColorScale? Scale, Color Low, Color High) GetColors()
    {
        if (this._accessibility.HighContrast)
        {
            return (true, null, this._settings.UIElementColor(UIElementType.Window),
                this._settings.UIElementColor(UIElementType.WindowText));
        }

        var dark = this._owner.ActualTheme == ElementTheme.Dark;
        var low = dark ? Color.FromArgb(255, 38, 48, 65) : Color.FromArgb(255, 225, 237, 250);
        var high = dark ? Color.FromArgb(255, 126, 199, 255) : Color.FromArgb(255, 8, 82, 160);
        return (false, this._owner.ColorScale, low, high);
    }

    public void RefreshLegend()
    {
        var source = ChartLegendSource.For(this._owner);
        if (this._owner.Data is not { HasValues: true } data)
        {
            source.Update(ChartLegendSwatch.Box, []);
            this._legend?.Update(false);
            return;
        }

        var (highContrast, scale, low, high) = this.GetColors();
        var stops = scale?.Stops;
        var minimum = stops?[0].Value ?? data.Minimum;
        var maximum = stops?[^1].Value ?? data.Maximum;
        var count = scale is { IsStepped: true } ? Math.Min(scale.Stops.Count, 12) : minimum == maximum ? 1 : 7;
        var entries = new ChartLegendEntry[count];
        for (var index = 0; index < count; index++)
        {
            var fraction = count == 1 ? 0.5 : (double)index / (count - 1);
            var value = minimum * (1 - fraction) + maximum * fraction;
            var color = scale?.GetColor(value) ?? HeatmapColorScale.Interpolate(low, high, fraction);
            if (scale is { IsStepped: true })
            {
                var stop = scale.Stops[(int)Math.Round(fraction * (scale.Stops.Count - 1))];
                value = stop.Value;
                color = stop.Color;
            }

            entries[index] = new(
                ChartInspectorPresenter.FormatValue(value, this._owner.ValueFormat), new SolidColorBrush(color),
                Stroke: highContrast ? this._owner.Foreground : null);
        }

        var lowLabel = ChartInspectorPresenter.FormatValue(minimum, this._owner.ValueFormat);
        var highLabel = minimum == maximum
            ? null
            : ChartInspectorPresenter.FormatValue(maximum, this._owner.ValueFormat) + (scale?.IsStepped == true ? "+" : string.Empty);
        source.Update(ChartLegendSwatch.Box, entries, scale: new ChartLegendScale(lowLabel, highLabel));
        this._legend?.Update(this._owner.ShowLegend, limitHeight: false);
    }

    private float GetCornerRadius()
    {
        return double.IsFinite(this._owner.CellCornerRadius)
            ? (float)Math.Clamp(this._owner.CellCornerRadius, 0, Math.Min(this._layout.Width, this._layout.Height) / 2)
            : 0;
    }

    private int HitTest(Point pointer)
    {
        if (this._owner.Data is not { } data || !this._layout.TryHit(pointer, data, out var row, out var column))
        {
            return -1;
        }

        return data[row, column].HasValue ? row * data.ColumnCount + column : -1;
    }

    private int Move(int index, VirtualKey direction)
    {
        if (!this._layout.IsValid || this._owner.Data is not { } data)
        {
            return -1;
        }

        if (index < 0)
        {
            for (var start = 0; start < data.Count; start++)
            {
                if (data[start / data.ColumnCount, start % data.ColumnCount].HasValue)
                {
                    return start;
                }
            }

            return -1;
        }

        var row = index / data.ColumnCount;
        var column = index % data.ColumnCount;
        var dx = direction == VirtualKey.Left ? -1 : direction == VirtualKey.Right ? 1 : 0;
        var dy = direction == VirtualKey.Up ? -1 : direction == VirtualKey.Down ? 1 : 0;
        for (row += dy, column += dx; row >= 0 && row < data.RowCount && column >= 0 && column < data.ColumnCount; row += dy, column += dx)
        {
            if (data[row, column].HasValue)
            {
                return row * data.ColumnCount + column;
            }
        }

        return -1;
    }

    private ChartInspectionItem? ReadCell(int index)
    {
        if (!this._layout.IsValid || this._owner.Data is not { } data || index < 0 || index >= data.Count)
        {
            return null;
        }

        var row = index / data.ColumnCount;
        var column = index % data.ColumnCount;
        if (data[row, column] is not { } cell)
        {
            return null;
        }

        var bounds = this._layout.Cell(row, column);
        var (highContrast, scale, low, high) = this.GetColors();
        var swatch = new SolidColorBrush(GetCellColor(data, cell, scale, low, high));
        return new ChartInspectionItem(
            new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2), (ChartInspectionRow[])
            [
                new(GetLabel(data, row, column, cell), swatch, highContrast ? new SolidColorBrush(high) : null),
                new(ChartInspectorPresenter.FormatValue(cell.Value, this._owner.ValueFormat))
            ]);
    }

    private ChartContextValue[] ReadContext(int index)
    {
        var data = this._owner.Data!;
        var row = index / data.ColumnCount;
        var column = index % data.ColumnCount;
        var cell = data[row, column]!.Value;
        return
        [
            new ChartContextValue(
                GetLabel(data, row, column, cell), cell.Value, cell, SeriesIndex: row, CategoryIndex: column, X: column,
                Timestamp: cell.Timestamp)
        ];
    }

    private static string GetLabel(HeatmapChartData data, int row, int column, HeatmapChartCell cell)
    {
        if (!string.IsNullOrEmpty(cell.Label))
        {
            return cell.Label;
        }

        if (cell.Timestamp is { } time)
        {
            return time.ToString("D", CultureInfo.CurrentCulture);
        }

        var rowLabel = data.RowLabels.Count == 0 ? null : data.RowLabels[row];
        var columnLabel = data.ColumnLabels.Count == 0 ? null : data.ColumnLabels[column];
        return WinUIResourceStrings.FormatDefault("ListPairFormat", rowLabel ?? WinUIResourceStrings.FormatDefault("HeatmapRowFormat", row + 1),
            columnLabel ?? WinUIResourceStrings.FormatDefault("HeatmapColumnFormat", column + 1));
    }

    private void Highlight(int index)
    {
        this._selected = index;
        var visible = index >= 0 && this._layout.IsValid && this._owner.Data is { } data && index < data.Count;
        this._selection.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (visible)
        {
            var columns = this._owner.Data!.ColumnCount;
            var bounds = this._layout.Cell(index / columns, index % columns);
            this._selection.Stroke = this._owner.Foreground;
            this._selection.Width = bounds.Width;
            this._selection.Height = bounds.Height;
            this._selection.RadiusX = this._selection.RadiusY = this.GetCornerRadius();
            Canvas.SetLeft(this._selection, bounds.Left);
            Canvas.SetTop(this._selection, bounds.Top);
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs args)
    {
        this.Render();
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        this.CreateSurface();
        this.Render();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        this.RemoveSurface();
    }

    private void CreateSurface()
    {
        if (this._surface is null)
        {
            this._surface = new CanvasControl { IsHitTestVisible = false };
            this._surface.Draw += this.OnDraw;
            this._plot.Children.Insert(0, this._surface);
        }
    }

    private void RemoveSurface()
    {
        if (this._surface is not { } surface)
        {
            return;
        }

        this._surface = null;
        surface.Draw -= this.OnDraw;
        this._plot.Children.Remove(surface);
        surface.RemoveFromVisualTree();
    }
}
