// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class FlowChartPresenter
{
    private readonly Control _owner;
    private readonly Canvas _plot;
    private readonly Action<FlowChartPresenter, Rect> _draw;
    private readonly ChartInspectorPresenter _inspector;
    private readonly AccessibilitySettings _accessibility = new();
    private readonly UISettings _settings = new();
    private readonly List<Mark> _marks = [];
    private readonly ChartLegendHighlight _seriesHighlight = new();
    private int _highlighted = -1;

    public FlowChartPresenter(Control owner, Canvas plot, Popup? popup, Action<FlowChartPresenter, Rect> draw)
    {
        this._owner = owner;
        this._plot = plot;
        this._draw = draw;
        this._inspector = new(
            owner, plot, popup, () => this._marks.Count, this.HitTest, index => this._marks[index].Inspection, this.Highlight,
            readContext: index => [this._marks[index].Value]);
        this._plot.SizeChanged += this.OnSizeChanged;
    }

    public static void ObserveAppearance(Control owner, Action render)
    {
        owner.ActualThemeChanged += (_, _) => render();
        owner.RegisterPropertyChangedCallback(ChartPalette.ColorsProperty, OnChanged);
        owner.RegisterPropertyChangedCallback(Control.ForegroundProperty, OnChanged);
        owner.RegisterPropertyChangedCallback(Control.FontFamilyProperty, OnChanged);
        owner.RegisterPropertyChangedCallback(Control.FontSizeProperty, OnChanged);

        void OnChanged(DependencyObject sender, DependencyProperty property)
        {
            render();
        }
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
        this._marks.Clear();
        this._seriesHighlight.ClearMarks();
        this._highlighted = -1;

        var bounds = new Rect(0, 0, this._plot.ActualWidth, this._plot.ActualHeight);
        this._plot.Clip = new RectangleGeometry { Rect = bounds };
        if (bounds.Width > 8 && bounds.Height > 8)
        {
            this._draw(this, bounds);
        }

        if (this._marks.Count == 0)
        {
            this._inspector.Clear();
        }
        else
        {
            this._inspector.Refresh();
        }
    }

    public Brush Fill(int index, Brush? brush)
    {
        return this._accessibility.HighContrast
            ? new SolidColorBrush(this._settings.UIElementColor(UIElementType.Highlight))
            : brush ?? new SolidColorBrush(ChartPalette.GetEffectiveColors(this._owner).GetColor(index, this._owner.ActualTheme));
    }

    public void AddMark(
        Geometry geometry,
        Rect bounds,
        Brush fill,
        ChartInspectionItem inspection,
        ChartContextValue value,
        double opacity = 1,
        Func<Point, bool>? contains = null)
    {
        var path = new Path { Data = geometry, Fill = fill, Opacity = opacity };
        AutomationProperties.SetName(path, inspection.Text);
        this._marks.Add(new Mark(path, bounds, inspection, value, opacity, contains));
        this._plot.Children.Add(path);
    }

    public void HighlightSeries(int index)
    {
        this._seriesHighlight.Select(index);
    }

    public void AddSeriesShape(Geometry geometry, Brush fill, int index)
    {
        var path = new Path { Data = geometry, Fill = fill, IsHitTestVisible = false };
        AutomationProperties.SetAccessibilityView(path, AccessibilityView.Raw);
        this._seriesHighlight.Add(path, index);
        this._plot.Children.Add(path);
    }

    public void AddLabel(string text, double left, double centerY, double width, TextAlignment alignment)
    {
        if (width <= 0)
        {
            return;
        }

        var label = new TextBlock
        {
            Text = text,
            Width = width,
            TextAlignment = alignment,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1,
            Foreground = this._owner.Foreground,
            FontFamily = this._owner.FontFamily,
            FontSize = this._owner.FontSize,
            IsHitTestVisible = false,
        };
        AutomationProperties.SetAccessibilityView(label, AccessibilityView.Raw);
        label.Measure(new Size(width, double.PositiveInfinity));
        Canvas.SetLeft(label, left);
        Canvas.SetTop(label, centerY - label.DesiredSize.Height / 2);
        this._plot.Children.Add(label);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs args)
    {
        this.Render();
    }

    private int HitTest(Point point)
    {
        for (var index = this._marks.Count - 1; index >= 0; index--)
        {
            var mark = this._marks[index];
            if (mark.Bounds.Contains(point) && (mark.Contains is null || mark.Contains(point)))
            {
                return index;
            }
        }

        return -1;
    }

    private void Highlight(int index)
    {
        if (this._highlighted >= 0)
        {
            var previous = this._marks[this._highlighted];
            previous.Path.Stroke = null;
            previous.Path.Opacity = previous.Opacity;
        }

        this._highlighted = (uint)index < (uint)this._marks.Count ? index : -1;
        if (this._highlighted >= 0)
        {
            var mark = this._marks[this._highlighted];
            mark.Path.Stroke = this._owner.Foreground;
            mark.Path.StrokeThickness = 1.5;
            mark.Path.Opacity = 1;
        }
    }

    private sealed record Mark(
        Path Path,
        Rect Bounds,
        ChartInspectionItem Inspection,
        ChartContextValue Value,
        double Opacity,
        Func<Point, bool>? Contains);
}

internal sealed partial class FlowChartAutomationPeer(Control owner, string className, string name) : FrameworkElementAutomationPeer(owner)
{
    protected override string GetClassNameCore()
    {
        return className;
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Group;
    }

    protected override string GetNameCore()
    {
        return base.GetNameCore() is { Length: > 0 } label ? label : name;
    }
}
