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

internal sealed class RadialGaugePresenter
{
    private readonly RadialGauge _owner;
    private readonly Canvas _plot;
    private readonly FrameworkElement? _labels;
    private readonly TranslateTransform? _labelOffset;
    private readonly Path _track = CreatePath(PenLineCap.Round);
    private readonly Path _indicator = CreatePath(PenLineCap.Round);
    private readonly Line _needle = new() { StrokeThickness = 2, IsHitTestVisible = false };
    private readonly List<Path> _bands = [];
    private readonly List<BandMark> _bandMarks = [];
    private readonly ChartInspectorPresenter _inspector;
    private readonly AccessibilitySettings _accessibility = new();
    private SolidColorBrush? _activeBandBrush;
    private Point _center;
    private double _radius;
    private double _start;
    private double _sweep;
    private double _thickness;
    private double _bandRadius;
    private double _bandThickness;

    public RadialGaugePresenter(RadialGauge owner, Canvas plot, Popup? popup, FrameworkElement? labels)
    {
        this._owner = owner;
        this._plot = plot;
        if (labels?.RenderTransform is TranslateTransform offset)
        {
            this._labels = labels;
            this._labelOffset = offset;
            labels.SizeChanged += this.OnLabelsSizeChanged;
        }

        plot.Children.Add(this._track);
        plot.Children.Add(this._indicator);
        plot.Children.Add(this._needle);
        plot.SizeChanged += this.OnSizeChanged;
        this._inspector = new ChartInspectorPresenter(
            owner, plot, popup, () => this._bandMarks.Count + 1, this.HitTest, this.ReadItem, readContext: this.ReadContext);
    }

    public void Detach()
    {
        this._inspector.Detach();
        this._plot.SizeChanged -= this.OnSizeChanged;
        if (this._labels is not null)
        {
            this._labels.SizeChanged -= this.OnLabelsSizeChanged;
        }

        this._plot.Children.Remove(this._track);
        this._plot.Children.Remove(this._indicator);
        this._plot.Children.Remove(this._needle);
        this.ClearBands();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        this.Render();
    }

    private void OnLabelsSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (this._owner.IsValueLabelCentered)
        {
            this.Render();
        }
    }

    public void Render()
    {
        this.ClearBands();
        var width = this._plot.ActualWidth;
        var height = this._plot.ActualHeight;
        var available = Math.Min(width / 2, height);
        var requested = this._owner.ArcThickness;
        this._thickness = double.IsFinite(requested) && requested > 0
            ? Math.Min(requested, available * 0.45)
            : Math.Min(14, available * 0.14);
        var inset = this._thickness / 2 + 4;
        this._start = this._owner.EffectiveStartAngle * (Math.PI / 180);
        this._sweep = this._owner.EffectiveSweepAngle * (Math.PI / 180);
        var centeredLabels = this._owner.IsValueLabelCentered ? this._labels : null;
        if (this._owner.UsesSemicircleLayout && centeredLabels is null)
        {
            this._radius = Math.Min(width / 2 - inset, height - 2 * inset);
            this._center = new Point(width / 2, (height + this._radius) / 2);
        }
        else
        {
            this.FitArc(width, height, inset, centeredLabels);
        }

        if (this._labelOffset is not null)
        {
            this._labelOffset.X = centeredLabels is null ? 0 : this._center.X - centeredLabels.ActualWidth / 2;
            this._labelOffset.Y = centeredLabels is null ? 0 : this._center.Y - centeredLabels.ActualHeight / 2;
        }

        this._track.Stroke = this._owner.TrackBrush ?? this._owner.BorderBrush;
        this._track.StrokeThickness = this._thickness;
        this._track.Data = this._radius > 0 ? this.CreateArc(this._radius, 0, 1) : null;
        this._indicator.StrokeThickness = this._thickness;
        this._needle.Stroke = this._owner.Foreground;

        this._bandThickness = Math.Min(6, this._thickness / 2);
        this._bandRadius = this._radius - (this._thickness + this._bandThickness) / 2 - 5;
        if (this._bandRadius > 0 && this._owner.Maximum > this._owner.Minimum)
        {
            this.RenderBands();
        }

        this.UpdateValue();
        this._inspector.Refresh();
    }

    public void UpdateValue()
    {
        if (this._radius <= 0)
        {
            this._indicator.Data = null;
            this._needle.Visibility = Visibility.Collapsed;
            return;
        }

        var position = this.Normalize(this._owner.Value);
        this._indicator.Stroke = this.GetIndicatorBrush();
        this._indicator.Data = position > 0 ? this.CreateValueArc(this._radius, 0, position) : null;
        var extension = this._thickness / 2 + 2;
        var arcPosition = this.MapDirection(position);
        var inner = this.AtPosition(Math.Max(0, this._radius - extension), arcPosition);
        var outer = this.AtPosition(this._radius + extension, arcPosition);
        this._needle.X1 = inner.X;
        this._needle.Y1 = inner.Y;
        this._needle.X2 = outer.X;
        this._needle.Y2 = outer.Y;
        this._needle.Visibility = Visibility.Visible;
    }

    private Brush? GetIndicatorBrush()
    {
        if (!this._owner.UseActiveBandColor || this._accessibility.HighContrast || this._owner.GetActiveBand() is not { } band)
        {
            return this._owner.IndicatorBrush ?? this._owner.Foreground;
        }

        if (this._activeBandBrush?.Color != band.Color)
        {
            this._activeBandBrush = new SolidColorBrush(band.Color);
        }

        return this._activeBandBrush;
    }

    private void ClearBands()
    {
        foreach (var path in this._bands)
        {
            this._plot.Children.Remove(path);
        }

        this._bands.Clear();
        this._bandMarks.Clear();
    }

    private void RenderBands()
    {
        var highContrast = this._accessibility.HighContrast;
        foreach (var band in this._owner.Bands)
        {
            var start = this.Normalize(band.Minimum);
            var end = this.Normalize(band.Maximum);
            if (end <= start)
            {
                continue;
            }

            // Leave a small separation without consuming narrow bands.
            var gap = Math.Min(1 / (this._sweep * this._bandRadius), (end - start) / 4);
            var path = CreatePath(PenLineCap.Flat);
            path.Stroke = highContrast ? this._owner.Foreground : new SolidColorBrush(band.Color);
            path.StrokeThickness = this._bandThickness;
            path.Data = this.CreateValueArc(this._bandRadius, start + gap, end - gap);
            if (highContrast && this._bands.Count % 2 != 0)
            {
                path.StrokeDashArray = new DoubleCollection { 2, 1.5 };
            }

            this._bands.Add(path);
            this._bandMarks.Add(new BandMark(band, start + gap, end - gap));
            this._plot.Children.Insert(this._plot.Children.Count - 1, path);
        }
    }

    public void RefreshInspection()
    {
        this._inspector.Refresh();
    }

    private int HitTest(Point point)
    {
        if (this._radius <= 0 || !new Rect(0, 0, this._plot.ActualWidth, this._plot.ActualHeight).Contains(point))
        {
            return -1;
        }

        var x = point.X - this._center.X;
        var y = point.Y - this._center.Y;
        var distance = Math.Sqrt(x * x + y * y);
        if (Math.Abs(distance - this._bandRadius) <= this._bandThickness / 2 + 3)
        {
            var arcPosition = this.OffsetFromStart(Math.Atan2(y, this._owner.IsMirrored ? -x : x)) / this._sweep;
            var position = this.MapDirection(arcPosition);
            var band = this._bandMarks.FindIndex(mark => position >= mark.Start && position <= mark.End);
            if (band >= 0)
            {
                return band + 1;
            }
        }

        return 0;
    }

    private ChartInspectionItem? ReadItem(int index)
    {
        if (this._radius <= 0)
        {
            return null;
        }

        if (index == 0)
        {
            var value = this._owner.FormatValue(this._owner.Value);
            var minimum = this._owner.FormatValue(this._owner.Minimum);
            var maximum = this._owner.FormatValue(this._owner.Maximum);
            var status = this._owner.GetActiveBand()?.Label;
            var text = string.IsNullOrWhiteSpace(status) ? $"Value: {value}" : $"Value: {value} ({status})";
            return new ChartInspectionItem(
                this.AtPosition(this._radius, this.MapDirection(this.Normalize(this._owner.Value))),
                (ChartInspectionRow[])[new(text, this._indicator.Stroke), new($"Range: {minimum} - {maximum}")]);
        }

        var mark = this._bandMarks[index - 1];
        var from = this._owner.FormatValue(Math.Max(this._owner.Minimum, mark.Band.Minimum));
        var to = this._owner.FormatValue(Math.Min(this._owner.Maximum, mark.Band.Maximum));
        var label = string.IsNullOrWhiteSpace(mark.Band.Label) ? "Threshold" : mark.Band.Label;
        return new ChartInspectionItem(
                this.AtPosition(this._bandRadius, this.MapDirection((mark.Start + mark.End) / 2)), $"{label}: {from} - {to}")
            .WithSwatch(this._bands[index - 1].Stroke);
    }

    private ChartContextValue[] ReadContext(int index)
    {
        if (index == 0)
        {
            return [new ChartContextValue("Value", this._owner.Value)];
        }

        var band = this._bandMarks[index - 1].Band;
        var label = string.IsNullOrWhiteSpace(band.Label) ? "Threshold" : band.Label;
        return [new ChartContextValue(label, null, band)];
    }

    private double Normalize(double value)
    {
        var minimum = this._owner.Minimum;
        var maximum = this._owner.Maximum;
        if (!double.IsFinite(minimum) || !double.IsFinite(maximum) || maximum <= minimum || double.IsNaN(value))
        {
            return 0;
        }

        var range = maximum - minimum;
        var bounded = Math.Clamp(value, minimum, maximum);
        return double.IsFinite(range) ? (bounded - minimum) / range : ((bounded / 2) - (minimum / 2)) / ((maximum / 2) - (minimum / 2));
    }

    private void FitArc(double width, double height, double inset, FrameworkElement? labels)
    {
        var left = 0d;
        var right = 0d;
        var top = 0d;
        var bottom = 0d;
        Include(this.UnitPoint(this._start));
        Include(this.UnitPoint(this._start + this._sweep));

        for (var quarter = 0; quarter < 4; quarter++)
        {
            var angle = quarter * (Math.PI / 2);
            if (this.OffsetFromStart(angle) <= this._sweep)
            {
                Include(this.UnitPoint(angle));
            }
        }

        var halfLabelWidth = labels is null ? 0 : Math.Min(width / 2, labels.ActualWidth / 2 + 4);
        var halfLabelHeight = labels is null ? 0 : Math.Min(height / 2, labels.ActualHeight / 2 + 4);
        this._radius = Math.Min(FitAxis(width, left, right, halfLabelWidth), FitAxis(height, top, bottom, halfLabelHeight));
        this._center = new Point(
            Math.Clamp((width - this._radius * (right + left)) / 2, halfLabelWidth, width - halfLabelWidth),
            Math.Clamp((height - this._radius * (bottom + top)) / 2, halfLabelHeight, height - halfLabelHeight));

        double FitAxis(double size, double min, double max, double labelHalf)
        {
            var available = Math.Max(0, size - 2 * inset);
            var radius = max > min ? available / (max - min) : available;
            var reach = Math.Max(-min, max);
            return labelHalf > 0 && reach > 0 ? Math.Min(radius, Math.Max(0, size - inset - labelHalf) / reach) : radius;
        }

        void Include(Point point)
        {
            left = Math.Min(left, point.X);
            right = Math.Max(right, point.X);
            top = Math.Min(top, point.Y);
            bottom = Math.Max(bottom, point.Y);
        }
    }

    private double OffsetFromStart(double angle)
    {
        var offset = (angle - this._start) % Math.Tau;
        return offset < 0 ? offset + Math.Tau : offset;
    }

    private Point UnitPoint(double angle)
    {
        return new((this._owner.IsMirrored ? -1 : 1) * Math.Cos(angle), Math.Sin(angle));
    }

    private Point AtPosition(double radius, double position)
    {
        var unit = this.UnitPoint(this._start + this._sweep * position);
        return new Point(this._center.X + radius * unit.X, this._center.Y + radius * unit.Y);
    }

    private double MapDirection(double position)
    {
        return this._owner.IsDirectionReversed ? 1 - position : position;
    }

    private PathGeometry CreateValueArc(double radius, double start, double end)
    {
        return this.CreateArc(radius, this.MapDirection(start), this.MapDirection(end));
    }

    private PathGeometry CreateArc(double radius, double start, double end)
    {
        var span = this._sweep * Math.Abs(end - start);
        var counterclockwise = this._owner.IsMirrored ^ (end < start);
        var figure = new PathFigure { StartPoint = this.AtPosition(radius, start), IsFilled = false, IsClosed = span == Math.Tau, };
        if (span > Math.PI)
        {
            AddSegment(this.AtPosition(radius, (start + end) / 2));
        }

        AddSegment(figure.IsClosed ? figure.StartPoint : this.AtPosition(radius, end));
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;

        void AddSegment(Point endPoint)
        {
            figure.Segments.Add(
                new ArcSegment
                {
                    Point = endPoint,
                    Size = new Size(radius, radius),
                    SweepDirection = counterclockwise ? SweepDirection.Counterclockwise : SweepDirection.Clockwise,
                });
        }
    }

    private static Path CreatePath(PenLineCap cap)
    {
        return new()
        {
            Stretch = Stretch.None, StrokeStartLineCap = cap, StrokeEndLineCap = cap, IsHitTestVisible = false,
        };
    }

    private readonly record struct BandMark(RadialGaugeBand Band, double Start, double End);
}
