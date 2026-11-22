// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal static class ChartReferenceLineRenderer
{
    public static void Draw(
        Canvas plot,
        Rect bounds,
        ChartScale scale,
        IReadOnlyList<ChartReferenceLine>? references,
        bool vertical,
        Brush foreground,
        FontFamily fontFamily,
        bool highContrast,
        long? timeOriginTicks = null)
    {
        if (references is not { Count: > 0 })
        {
            return;
        }

        List<Rect>? captions = null;
        foreach (var reference in references)
        {
            if (reference.ProjectValue(timeOriginTicks) is not { } value || !scale.Contains(value))
            {
                continue;
            }

            var fraction = scale.Normalize(value);
            var coordinate = vertical ? bounds.Left + fraction * bounds.Width : bounds.Bottom - fraction * bounds.Height;
            var line = new Line
            {
                X1 = vertical ? coordinate : bounds.Left,
                X2 = vertical ? coordinate : bounds.Right,
                Y1 = vertical ? bounds.Top : coordinate,
                Y2 = vertical ? bounds.Bottom : coordinate,
                Stroke = highContrast ? foreground : reference.Stroke ?? foreground,
                StrokeThickness = reference.StrokeThickness,
                Opacity = highContrast || reference.Stroke is not null ? 1 : 0.78,
                IsHitTestVisible = false,
                Clip = new RectangleGeometry { Rect = bounds },
            };
            if (reference.IsStrokeDashed)
            {
                line.StrokeDashArray = [4, 3];
            }

            plot.Children.Add(line);
            if (!string.IsNullOrWhiteSpace(reference.Label))
            {
                DrawLabel(plot, bounds, reference.Label, foreground, fontFamily, vertical, coordinate, captions ??= []);
            }
        }
    }

    private static void DrawLabel(
        Canvas plot,
        Rect bounds,
        string text,
        Brush foreground,
        FontFamily fontFamily,
        bool vertical,
        double coordinate,
        List<Rect> captions)
    {
        const double inset = 6;
        const double gap = 4;
        var availableWidth = bounds.Width - 2 * inset;
        if (availableWidth <= 0)
        {
            return;
        }

        var label = new TextBlock
        {
            Text = text,
            Foreground = foreground,
            FontFamily = fontFamily,
            FontSize = 12,
            MaxLines = 1,
            TextTrimming = Microsoft.UI.Xaml.TextTrimming.CharacterEllipsis,
            IsHitTestVisible = false,
        };
        label.Measure(new Size(availableWidth, double.PositiveInfinity));
        var width = Math.Min(availableWidth, Math.Ceiling(label.DesiredSize.Width) + 1);
        var height = label.DesiredSize.Height;
        if (height + 2 * gap > bounds.Height)
        {
            return;
        }

        var x = bounds.Right - inset - width;
        var y = bounds.Top + gap;
        if (vertical)
        {
            x = coordinate + inset + width <= bounds.Right - inset ? coordinate + inset : coordinate - inset - width;
            x = Math.Clamp(x, bounds.Left + inset, bounds.Right - inset - width);
        }
        else
        {
            var above = coordinate - height - gap;
            y = above >= bounds.Top ? above : Math.Min(coordinate + gap, bounds.Bottom - height);
        }

        var area = new Rect(x, y, width, height);
        foreach (var existing in captions)
        {
            if (area.Left < existing.Right && area.Right > existing.Left && area.Top < existing.Bottom && area.Bottom > existing.Top)
            {
                return;
            }
        }

        captions.Add(area);
        label.Width = width;
        Canvas.SetLeft(label, x);
        Canvas.SetTop(label, y);
        plot.Children.Add(label);
    }
}
