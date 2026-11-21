// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class SunburstChartPresenter
{
    private readonly SunburstChart _owner;
    private readonly Canvas _plot;
    private readonly ChartInspectorPresenter _inspector;
    private readonly AccessibilitySettings _accessibility = new();
    private readonly UISettings _settings = new();
    private readonly Canvas _ringLabels = new() { IsHitTestVisible = false };
    private readonly List<Path> _segmentPaths = [];
    private readonly Path _outlineOuter = MakePath(4);
    private readonly Path _outlineInner = MakePath(1.5);
    private List<SunburstTile> _tiles = [];
    private int _highlightedIndex = -1;

    public SunburstChartPresenter(SunburstChart owner, Canvas plot, Popup? popup, Func<HierarchyChartNode, bool> invoke)
    {
        this._owner = owner;
        this._plot = plot;
        this._inspector = new ChartInspectorPresenter(
            owner, plot, popup, () => this._tiles.Count, this.HitTest, this.ReadTile, this.Highlight,
            invoke: index => invoke(this._tiles[index].Partition.Node), readContext: index =>
            {
                var node = this._tiles[index].Partition.Node;
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
        var index = this.FindTile(key);
        if (index < 0 || !this._owner.IsLoaded)
        {
            bounds = Rect.Empty;
            return false;
        }

        bounds = this._plot.TransformToVisual(this._owner).TransformBounds(this._tiles[index].Sector.GetBounds());
        return bounds.Width > 0 && bounds.Height > 0;
    }

    public void Render()
    {
        var index = this._inspector.FocusedIndex;
        var selected = index >= 0 ? this._tiles[index].Partition.Node.Key : null;
        this._plot.Children.Clear();
        this._ringLabels.Children.Clear();
        this._segmentPaths.Clear();
        var bounds = new Rect(0, 0, this._plot.ActualWidth, this._plot.ActualHeight);
        var depth = Math.Clamp(this._owner.MaxDepth, 1, 32);
        this._tiles = this._owner.Data is { } data ? SunburstLayout.Create(data.Root, bounds, depth, this._owner.EffectiveStartAngle) : [];
        if (this._tiles.Count == 0)
        {
            this._inspector.Clear();
            return;
        }

        this._plot.Clip = new RectangleGeometry { Rect = bounds };
        var highContrast = this._accessibility.HighContrast;
        var theme = this._owner.ActualTheme;
        var palette = ChartPalette.GetEffectiveColors(this._owner);
        var surface = new SolidColorBrush(
            highContrast ? this._settings.UIElementColor(UIElementType.Window) : HierarchyChartPalette.Surface(theme));
        var foreground = highContrast
            ? new SolidColorBrush(this._settings.UIElementColor(UIElementType.WindowText))
            : this._owner.Foreground;
        var fills = new Dictionary<(int Branch, int Depth), SolidColorBrush>();
        var fontFamily = this._owner.FontFamily.Source;
        var mirrorCoordinates = this._plot.FlowDirection == FlowDirection.RightToLeft;
        using var labelFormat = new CanvasTextFormat
        {
            FontFamily = fontFamily == "XamlAutoFontFamily" ? "Segoe UI" : fontFamily,
            FontSize = (float)(this._owner.FontSize * this._settings.TextScaleFactor),
            FontWeight = FontWeights.Normal,
            LocaleName = this._owner.Language,
            Direction = this._owner.FlowDirection == FlowDirection.RightToLeft
                ? CanvasTextDirection.RightToLeftThenTopToBottom
                : CanvasTextDirection.LeftToRightThenTopToBottom,
            WordWrapping = CanvasWordWrapping.NoWrap,
            TrimmingGranularity = CanvasTextTrimmingGranularity.Character,
            TrimmingSign = CanvasTrimmingSign.Ellipsis,
        };
        foreach (var (partition, sector) in this._tiles)
        {
            var fill = surface;
            if (!highContrast && partition.Branch >= 0)
            {
                var key = (partition.Branch, partition.Depth);
                if (!fills.TryGetValue(key, out var branchFill))
                {
                    var accent = palette.GetColor(partition.Branch, theme);
                    var tint = 0.27 + 0.075 * Math.Min(partition.Depth, 3);
                    branchFill = new SolidColorBrush(HierarchyChartPalette.Blend(surface.Color, accent, tint));
                    fills.Add(key, branchFill);
                }

                fill = branchFill;
            }

            var path = MakePath(highContrast ? 1 : 0);
            path.Data = sector.CreateGeometry();
            path.Fill = fill;
            path.Stroke = highContrast ? foreground : null;
            this._segmentPaths.Add(path);
            this._plot.Children.Add(path);

            if (partition.Depth == 0)
            {
                this.DrawCenter(partition.Node, sector, foreground);
            }
            else if (SunburstLabelGeometry.Create(partition.Node.Label, sector, labelFormat, mirrorCoordinates) is { } geometry)
            {
                var label = MakePath(0);
                label.Data = geometry;
                label.Fill = foreground;
                this._ringLabels.Children.Add(label);
            }
        }

        this._plot.Children.Add(this._ringLabels);
        this._outlineOuter.Stroke = surface;
        this._outlineInner.Stroke = foreground;
        this.Highlight(-1);
        this._plot.Children.Add(this._outlineOuter);
        this._plot.Children.Add(this._outlineInner);
        this._inspector.Refresh(selected is null ? -1 : this.FindTile(selected));
    }

    private void DrawCenter(HierarchyChartNode root, SunburstSector sector, Brush? foreground)
    {
        var labelHeight = this._owner.FontSize * 1.4;
        var width = sector.OuterRadius * 1.55;
        if (width < 36 || sector.OuterRadius * 1.15 < labelHeight)
        {
            return;
        }

        var showValue = sector.OuterRadius * 1.15 >= 2 * labelHeight + 2;
        var height = showValue ? 2 * labelHeight + 2 : labelHeight;
        var labels = new StackPanel { Spacing = 2, Width = width, Height = height, IsHitTestVisible = false };
        labels.Children.Add(this.CreateLabel(root.Label, width, labelHeight, foreground));
        if (showValue)
        {
            var value = ChartInspectorPresenter.FormatValue(root.Value, this._owner.ValueFormat);
            labels.Children.Add(this.CreateLabel(value, width, labelHeight, foreground));
        }

        Canvas.SetLeft(labels, sector.Center.X - width / 2);
        Canvas.SetTop(labels, sector.Center.Y - height / 2);
        this._plot.Children.Add(labels);
    }

    private TextBlock CreateLabel(string text, double width, double height, Brush? brush)
    {
        return new()
        {
            Text = text,
            Width = width,
            Height = height,
            FontFamily = this._owner.FontFamily,
            FontSize = this._owner.FontSize,
            FontWeight = FontWeights.Normal,
            Foreground = brush,
            TextAlignment = TextAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1,
            IsHitTestVisible = false,
        };
    }

    private static Path MakePath(double strokeThickness)
    {
        return new()
        {
            StrokeThickness = strokeThickness,
            StrokeLineJoin = PenLineJoin.Round,
            Stretch = Stretch.None,
            HighContrastAdjustment = ElementHighContrastAdjustment.None,
            IsHitTestVisible = false,
        };
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs args)
    {
        this.Render();
    }

    private int FindTile(string key)
    {
        return this._tiles.FindIndex(tile => tile.Partition.Node.Key == key);
    }

    private int HitTest(Point point)
    {
        return this._tiles.FindLastIndex(tile => tile.Sector.Contains(point));
    }

    private ChartInspectionItem? ReadTile(int index)
    {
        var (partition, sector) = this._tiles[index];
        var anchor = partition.Depth == 0 ? sector.Center : sector.At(sector.MidRadius, sector.MidAngle);
        var path = this._segmentPaths[index];
        return HierarchyChartReadout.Read(
            this._tiles, index, this._owner.Data!.Root, static tile => (tile.Partition.Node, tile.Partition.Parent), anchor,
            this._owner.ValueFormat).WithSwatch(path.Fill, path.Stroke);
    }

    private void Highlight(int index)
    {
        if (index >= 0 && index == this._highlightedIndex)
        {
            return;
        }

        this._highlightedIndex = index;
        var visible = index >= 0;
        this._outlineOuter.Data = visible ? this._tiles[index].Sector.CreateGeometry() : null;
        this._outlineInner.Data = visible ? this._tiles[index].Sector.CreateGeometry() : null;
        this._outlineOuter.Visibility = this._outlineInner.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }
}
