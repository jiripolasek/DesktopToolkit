// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal static class ChartMilestoneRenderer
{
    private const double Radius = 5;
    private const double Gap = 4;

    public static void Draw(
        Canvas plot,
        Control owner,
        CartesianPlotLayout layout,
        ChartMilestones? milestones,
        bool highContrast,
        long? timeOriginTicks = null)
    {
        if (milestones is not { Items.Count: > 0 })
        {
            return;
        }

        var bounds = layout.Bounds;
        var localBounds = new Rect(0, 0, bounds.Width, bounds.Height);
        Canvas? layer = null;
        List<Rect>? captions = null;
        foreach (var milestone in milestones.Items)
        {
            if (milestone.GetX(timeOriginTicks) is not { } x || !layout.X.Contains(x) || milestone.Y is { } y && !layout.Y.Contains(y))
            {
                continue;
            }

            layer ??= new Canvas
            {
                Name = "MilestoneLayer",
                Width = bounds.Width,
                Height = bounds.Height,
                Clip = new RectangleGeometry { Rect = localBounds },
                IsHitTestVisible = false,
            };
            var point = new Point(layout.MapX(x) - bounds.Left, milestone.Y is { } value ? layout.MapY(value) - bounds.Top : Radius + Gap);
            var brush = highContrast ? owner.Foreground : milestone.Brush ?? owner.Foreground;
            AddMarker(layer, point, brush, milestone.Y is null, milestone.Label);
            AddLabel(layer, owner, localBounds, point, milestone.Label, milestone.Y is null, captions ??= []);
        }

        if (layer is not null)
        {
            Canvas.SetLeft(layer, bounds.Left);
            Canvas.SetTop(layer, bounds.Top);
            plot.Children.Add(layer);
        }
    }

    private static void AddMarker(Canvas layer, Point point, Brush brush, bool isTimeline, string label)
    {
        if (isTimeline)
        {
            layer.Children.Add(
                new Line
                {
                    X1 = point.X,
                    X2 = point.X,
                    Y1 = point.Y + Radius,
                    Y2 = point.Y + Radius + 7,
                    Stroke = brush,
                    Opacity = 0.7,
                });
        }

        var marker = new Polygon
        {
            Points = [new(Radius, 0), new(2 * Radius, Radius), new(Radius, 2 * Radius), new(0, Radius)],
            Fill = isTimeline ? brush : null,
            Stroke = brush,
            StrokeThickness = 1.5,
        };
        Canvas.SetLeft(marker, point.X - Radius);
        Canvas.SetTop(marker, point.Y - Radius);
        AutomationProperties.SetName(marker, label);
        layer.Children.Add(marker);
    }

    private static void AddLabel(Canvas layer, Control owner, Rect bounds, Point point, string text, bool isTimeline, List<Rect> captions)
    {
        var widthLimit = bounds.Width - 2 * Gap;
        if (widthLimit <= 0)
        {
            return;
        }

        var label = new TextBlock
        {
            Text = text,
            Foreground = owner.Foreground,
            FontFamily = owner.FontFamily,
            FontSize = 12,
            MaxLines = 1,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        label.Measure(new Size(widthLimit, double.PositiveInfinity));
        var width = Math.Min(widthLimit, Math.Ceiling(label.DesiredSize.Width) + 1);
        var height = label.DesiredSize.Height;
        if (height > bounds.Height)
        {
            return;
        }

        var right = point.X + Radius + Gap;
        var left = point.X - Radius - Gap - width;
        var x = Math.Clamp(right + width <= bounds.Right - Gap ? right : left, Gap, bounds.Right - Gap - width);
        var top = isTimeline ? point.Y - height / 2 : point.Y - Radius - Gap - height;
        var y = Math.Clamp(top >= 0 ? top : isTimeline ? 0 : point.Y + Radius + Gap, 0, bounds.Bottom - height);
        var area = new Rect(x, y, width, height);
        if (captions.Any(other =>
                area.Left < other.Right + Gap
                && area.Right + Gap > other.Left
                && area.Top < other.Bottom + Gap
                && area.Bottom + Gap > other.Top))
        {
            return;
        }

        captions.Add(area);
        label.Width = width;
        Canvas.SetLeft(label, x);
        Canvas.SetTop(label, y);
        layer.Children.Add(label);
    }
}
