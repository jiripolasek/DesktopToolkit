// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class HierarchyChartPresenter
{
    private readonly List<(Brush Fill, Brush? Outline)> _swatches = [];
    private readonly Control _owner;
    private readonly Canvas _plot;
    private readonly Func<HierarchyChartData?> _data;
    private readonly Func<string?> _valueFormat;
    private readonly Func<HierarchyChartNode, Rect, double, List<HierarchyTile>> _layout;
    private readonly TreemapChart? _treemap;
    private readonly ChartInspectorPresenter _inspector;
    private readonly AccessibilitySettings _accessibility = new();
    private readonly UISettings _settings = new();
    private readonly Rectangle _outlineOuter = new() { IsHitTestVisible = false };
    private readonly Rectangle _outlineInner = new() { IsHitTestVisible = false };
    private List<HierarchyTile> _tiles = [];

    public HierarchyChartPresenter(
        Control owner,
        Canvas plot,
        Popup? popup,
        Func<HierarchyChartData?> data,
        Func<string?> valueFormat,
        Func<HierarchyChartNode, Rect, double, List<HierarchyTile>> layout,
        Func<HierarchyChartNode, bool> invoke)
    {
        this._owner = owner;
        this._plot = plot;
        this._data = data;
        this._valueFormat = valueFormat;
        this._layout = layout;
        this._treemap = owner as TreemapChart;
        this._inspector = new ChartInspectorPresenter(
            owner, plot, popup, () => this._tiles.Count, this.HitTest, this.ReadTile, this.Highlight,
            invoke: index => invoke(this._tiles[index].Node), readContext: index =>
            {
                var node = this._tiles[index].Node;
                return [new ChartContextValue(node.Label, node.Value, node)];
            });
        plot.SizeChanged += this.OnSizeChanged;
    }

    public void Detach()
    {
        this._plot.SizeChanged -= this.OnSizeChanged;
        this._inspector.Detach();
    }

    public bool TryGetTileBounds(string key, out Rect bounds)
    {
        var index = this._tiles.FindIndex(tile => tile.Node.Key == key);
        if (index < 0 || !this._owner.IsLoaded)
        {
            bounds = Rect.Empty;
            return false;
        }

        var tile = this._tiles[index].Bounds;
        bounds = this._plot.TransformToVisual(this._owner).TransformBounds(tile);
        return bounds.Width > 0 && bounds.Height > 0;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        this.Render();
    }

    public void Render()
    {
        var index = this._inspector.FocusedIndex;
        var selected = index >= 0 ? this._tiles[index].Node.Key : null;
        this._plot.Children.Clear();
        this._swatches.Clear();
        var width = this._plot.ActualWidth;
        var height = this._plot.ActualHeight;
        var root = this._data()?.Root;
        if (root is null || root.Value == 0 || width < 2 || height < 2)
        {
            this._tiles.Clear();
            this._inspector.Clear();
            return;
        }

        var highContrast = this._accessibility.HighContrast;
        var palette = ChartPalette.GetEffectiveColors(this._owner);
        var theme = this._owner.ActualTheme;
        var text = highContrast ? this.SystemBrush(UIElementType.WindowText) : this._owner.Foreground;
        var valueOpacity = highContrast ? 1 : this._treemap?.EffectiveValueLabelOpacity ?? 1;
        var showValues = (this._owner is IcicleChart icicle ? icicle.ShowValueLabels : this._treemap?.ShowValueLabels ?? true)
                         && valueOpacity > 0;
        var surface = highContrast ? this.SystemBrush(UIElementType.Window) : new SolidColorBrush(HierarchyChartPalette.Surface(theme));
        var brushes = new Dictionary<(int Branch, int Depth, bool Expanded), (Brush Fill, Brush? Outline)>();
        var bounds = new Rect(0, 0, width, height);
        this._plot.Clip = new RectangleGeometry { Rect = bounds };
        var labelHeight = this._owner.FontSize * 1.5;
        this._tiles = this._layout(root, bounds, labelHeight);
        foreach (var tile in this._tiles)
        {
            var key = (tile.Branch, tile.Depth, tile.IsExpanded);
            if (!brushes.TryGetValue(key, out var tileBrushes))
            {
                tileBrushes = GetTileBrushes(tile, surface, text, highContrast, palette, theme);
                brushes.Add(key, tileBrushes);
            }

            this._swatches.Add(tileBrushes);

            var radius = this.CornerRadius(tile);
            var block = new Rectangle
            {
                Fill = tileBrushes.Fill,
                Stroke = tileBrushes.Outline,
                StrokeThickness = tileBrushes.Outline is not null ? 1 : 0,
                RadiusX = radius,
                RadiusY = radius,
                HighContrastAdjustment = ElementHighContrastAdjustment.None,
                IsHitTestVisible = false,
            };
            this.Place(block, tile.Bounds);
            if (this._treemap is { } treemap && tile.IsExpanded)
            {
                this.DrawBranchHeader(tile, labelHeight, text, treemap.EffectiveBranchPadding, showValues, valueOpacity);
            }
            else if (tile.Bounds.Width >= 43 && tile.Bounds.Height >= labelHeight + 8)
            {
                this.DrawLabel(tile.Node.Label, text, tile, 4, labelHeight, strong: true);
                if (showValues && !tile.IsExpanded && tile.Bounds.Height >= 2 * labelHeight + 10)
                {
                    this.DrawLabel(
                        this.Format(tile.Node.Value), text, tile, labelHeight + 5, labelHeight, strong: false, opacity: valueOpacity);
                }
            }
        }

        this._outlineOuter.Stroke = surface;
        this._outlineInner.Stroke = text;
        this._outlineOuter.HighContrastAdjustment = this._outlineInner.HighContrastAdjustment = ElementHighContrastAdjustment.None;
        this.Highlight(-1);
        this._plot.Children.Add(this._outlineOuter);
        this._plot.Children.Add(this._outlineInner);
        this._inspector.Refresh(selected is null ? -1 : this._tiles.FindIndex(tile => tile.Node.Key == selected));
    }

    private static (Brush Fill, Brush? Outline) GetTileBrushes(
        HierarchyTile tile,
        SolidColorBrush surface,
        Brush? text,
        bool highContrast,
        ChartColorSet palette,
        ElementTheme theme)
    {
        if (highContrast || tile.Branch < 0)
        {
            return (surface, highContrast ? text : null);
        }

        var branch = palette.GetColor(tile.Branch, theme);
        var tint = tile.IsExpanded ? (tile.Depth == 1 ? 0.12 : 0.23) : 0.37 + ((tile.Depth % 3) * 0.04);
        var outline = tile.IsExpanded ? new SolidColorBrush(HierarchyChartPalette.Blend(surface.Color, branch, 0.42)) : null;
        return (new SolidColorBrush(HierarchyChartPalette.Blend(surface.Color, branch, tint)), outline);
    }

    private double CornerRadius(HierarchyTile tile)
    {
        if (this._treemap is not { } treemap)
        {
            return 0;
        }

        var limit = Math.Min(tile.Bounds.Width, tile.Bounds.Height) / 4;
        if (tile.IsExpanded)
        {
            // Keep children inside the rounded frame.
            limit = Math.Min(limit, 3 * treemap.EffectiveBranchPadding);
        }

        return Math.Min(treemap.GetTileCornerRadius(tile.IsExpanded), limit);
    }

    private void DrawBranchHeader(HierarchyTile tile, double labelHeight, Brush? text, double padding, bool showValues, double valueOpacity)
    {
        var bounds = tile.Bounds;
        var width = bounds.Width - 2 * padding;
        var row = new Grid { ColumnSpacing = 6, IsHitTestVisible = false };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.Children.Add(this.CreateLabel(tile.Node.Label, text, strong: true));
        if (showValues)
        {
            var value = this.CreateLabel(this.Format(tile.Node.Value), text, strong: false, opacity: valueOpacity);
            value.Measure(new Size(double.PositiveInfinity, labelHeight));
            if (width >= value.DesiredSize.Width + 48)
            {
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                Grid.SetColumn(value, 1);
                row.Children.Add(value);
            }
        }

        this.Place(row, new Rect(bounds.X + padding, bounds.Y + 4, width, labelHeight));
    }

    private void DrawLabel(string value, Brush? brush, HierarchyTile tile, double top, double height, bool strong, double opacity = 1)
    {
        var padding = this._treemap is null ? 6 : 8;
        this.Place(
            this.CreateLabel(value, brush, strong, opacity),
            new Rect(tile.Bounds.X + padding, tile.Bounds.Y + top, tile.Bounds.Width - 2 * padding, height));
    }

    private TextBlock CreateLabel(string value, Brush? brush, bool strong, double opacity = 1)
    {
        return new()
        {
            Text = value,
            FontFamily = this._owner.FontFamily,
            FontSize = this._owner.FontSize,
            FontWeight = strong ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = brush,
            Opacity = opacity,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1,
            IsHitTestVisible = false,
        };
    }

    private void Place(FrameworkElement visual, Rect bounds)
    {
        SizeAt(visual, bounds);
        this._plot.Children.Add(visual);
    }

    private static void SizeAt(FrameworkElement visual, Rect bounds)
    {
        visual.Width = bounds.Width;
        visual.Height = bounds.Height;
        Canvas.SetLeft(visual, bounds.X);
        Canvas.SetTop(visual, bounds.Y);
    }

    private int HitTest(Point point)
    {
        return this._tiles.FindLastIndex(tile => tile.Bounds.Contains(point));
    }

    private ChartInspectionItem? ReadTile(int index)
    {
        var tile = this._tiles[index];
        var anchor = new Point(tile.Bounds.Right, tile.Bounds.Top + Math.Min(14, tile.Bounds.Height / 2));
        var (fill, outline) = this._swatches[index];
        return HierarchyChartReadout.Read(
                this._tiles, index, this._data()!.Root, static item => (item.Node, item.Parent), anchor, this._valueFormat())
            .WithSwatch(fill, outline);
    }

    private void Highlight(int index)
    {
        var visibility = index >= 0 ? Visibility.Visible : Visibility.Collapsed;
        this._outlineInner.Visibility = this._outlineOuter.Visibility = visibility;
        if (index < 0)
        {
            return;
        }

        var bounds = this._tiles[index].Bounds;
        var radius = this.CornerRadius(this._tiles[index]);
        this._outlineOuter.RadiusX = this._outlineOuter.RadiusY = this._outlineInner.RadiusX = this._outlineInner.RadiusY = radius;
        var narrow = Math.Min(bounds.Width, bounds.Height);
        this._outlineOuter.StrokeThickness = Math.Min(4, narrow);
        this._outlineInner.StrokeThickness = Math.Min(1.5, narrow);
        SizeAt(this._outlineOuter, bounds);
        SizeAt(this._outlineInner, bounds);
    }

    private string Format(double value)
    {
        return ChartInspectorPresenter.FormatValue(value, this._valueFormat());
    }

    private SolidColorBrush SystemBrush(UIElementType element)
    {
        return new(this._settings.UIElementColor(element));
    }
}
