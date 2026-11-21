// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Windows.UI;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed partial class DoughnutSliceRenderer : IDisposable
{
    private const float StartAtTop = -MathF.PI / 2;
    private const float FullTurn = 2 * MathF.PI;
    private const float MinimumSweep = 0.0001f;
    private CanvasStrokeStyle? _strokeStyle;

    public int HighlightedSeries { get; set; } = -1;

    public void Dispose()
    {
        this._strokeStyle?.Dispose();
    }

    public void Draw(
        ICanvasResourceCreator resourceCreator,
        CanvasDrawingSession drawingSession,
        Vector2 size,
        DoughnutGraphMode mode,
        DoughnutGraphSeries[] series,
        double[] values,
        double ringThickness,
        double gapAngle,
        Color trackColor)
    {
        var pie = mode == DoughnutGraphMode.Pie;
        var (radius, strokeWidth) = GetDimensions(size, pie, ringThickness);
        if (radius <= 0 || (!pie && strokeWidth <= 0))
        {
            return;
        }

        var center = size / 2;
        DrawCircle(trackColor);
        if (values.Length != series.Length)
        {
            return;
        }

        var nonEmptyCount = values.Count(value => value > 0);
        if (nonEmptyCount == 0)
        {
            return;
        }

        if (nonEmptyCount == 1)
        {
            var index = Array.FindIndex(values, value => value > 0);
            var sweep = GetSweep(values[index]);
            if (sweep >= FullTurn - MinimumSweep)
            {
                DrawCircle(GetColor(index));
            }
            else
            {
                DrawSlice(radius, StartAtTop, sweep, GetColor(index));
            }

            return;
        }

        if (pie)
        {
            // Clip to a smooth circle and assign each interior pixel to one sector.
            using var clip = CanvasGeometry.CreateCircle(resourceCreator, center, radius);
            using var layer = drawingSession.CreateLayer(1, clip);
            var originalAntialiasing = drawingSession.Antialiasing;
            var originalBlend = drawingSession.Blend;
            drawingSession.Antialiasing = CanvasAntialiasing.Aliased;
            drawingSession.Blend = CanvasBlend.Copy;
            try
            {
                DrawSlices(radius + drawingSession.ConvertPixelsToDips(2), 0);
            }
            finally
            {
                drawingSession.Blend = originalBlend;
                drawingSession.Antialiasing = originalAntialiasing;
            }
        }
        else
        {
            DrawSlices(radius, (float)(gapAngle * Math.PI / 180d));
        }

        void DrawSlices(float sliceRadius, float gapRadians)
        {
            var currentAngle = StartAtTop;
            for (var index = 0; index < values.Length; index++)
            {
                var sweep = GetSweep(values[index]);
                var (start, visibleSweep) = TrimGap(currentAngle, sweep, gapRadians);
                if (visibleSweep > MinimumSweep)
                {
                    DrawSlice(sliceRadius, start, visibleSweep, GetColor(index));
                }

                currentAngle += sweep;
            }
        }

        Color GetColor(int index)
        {
            var color = series[index].Color;
            var opacity = ChartLegendHighlight.OpacityFor(index, this.HighlightedSeries);
            return Color.FromArgb((byte)(color.A * opacity), color.R, color.G, color.B);
        }

        void DrawCircle(Color color)
        {
            if (pie)
            {
                drawingSession.FillCircle(center, radius, color);
            }
            else
            {
                drawingSession.DrawCircle(center, radius, color, strokeWidth);
            }
        }

        void DrawSlice(float sliceRadius, float startAngle, float sweepAngle, Color color)
        {
            using var pathBuilder = new CanvasPathBuilder(resourceCreator);
            var startPoint = center + new Vector2(MathF.Cos(startAngle), MathF.Sin(startAngle)) * sliceRadius;
            pathBuilder.BeginFigure(pie ? center : startPoint);
            if (pie)
            {
                pathBuilder.AddLine(startPoint);
            }

            pathBuilder.AddArc(center, sliceRadius, sliceRadius, startAngle, sweepAngle);
            pathBuilder.EndFigure(pie ? CanvasFigureLoop.Closed : CanvasFigureLoop.Open);
            using var geometry = CanvasGeometry.CreatePath(pathBuilder);
            if (pie)
            {
                drawingSession.FillGeometry(geometry, color);
            }
            else
            {
                this._strokeStyle ??= new CanvasStrokeStyle { StartCap = CanvasCapStyle.Flat, EndCap = CanvasCapStyle.Flat };
                drawingSession.DrawGeometry(geometry, color, strokeWidth, this._strokeStyle);
            }
        }
    }

    public static int HitTest(Vector2 point, Vector2 size, DoughnutGraphMode mode, double[] values, double ringThickness, double gapAngle)
    {
        var pie = mode == DoughnutGraphMode.Pie;
        var (radius, strokeWidth) = GetDimensions(size, pie, ringThickness);
        var relative = point - size / 2;
        var distance = relative.Length();
        if (radius <= 0 || distance > radius + strokeWidth / 2 || (!pie && (strokeWidth <= 0 || distance < radius - strokeWidth / 2)))
        {
            return -1;
        }

        var angle = MathF.Atan2(relative.Y, relative.X);
        if (angle < StartAtTop)
        {
            angle += FullTurn;
        }

        var singleSlice = values.Count(value => value > 0) == 1;
        var gap = pie || singleSlice ? 0 : (float)(gapAngle * Math.PI / 180d);
        var currentAngle = StartAtTop;
        for (var index = 0; index < values.Length; index++)
        {
            var sweep = GetSweep(values[index]);
            var (start, visibleSweep) = TrimGap(currentAngle, sweep, gap);
            if ((singleSlice ? sweep > 0 : visibleSweep > MinimumSweep)
                && (singleSlice && sweep >= FullTurn - MinimumSweep || (angle >= start && angle < start + visibleSweep)))
            {
                return index;
            }

            currentAngle += sweep;
        }

        return -1;
    }

    public static Vector2? GetAnchor(int index, Vector2 size, DoughnutGraphMode mode, double[] values, double ringThickness)
    {
        var pie = mode == DoughnutGraphMode.Pie;
        var (radius, strokeWidth) = GetDimensions(size, pie, ringThickness);
        var sweep = GetSweep(values[index]);
        if (radius <= 0 || (!pie && strokeWidth <= 0) || sweep <= 0)
        {
            return null;
        }

        var middle = StartAtTop + sweep / 2;
        for (var preceding = 0; preceding < index; preceding++)
        {
            middle += GetSweep(values[preceding]);
        }

        return size / 2 + new Vector2(MathF.Cos(middle), MathF.Sin(middle)) * radius;
    }

    private static (float Radius, float StrokeWidth) GetDimensions(Vector2 size, bool pie, double ringThickness)
    {
        var diameter = Math.Min(size.X, size.Y);
        var strokeWidth = pie ? 0 : Math.Max(0, Math.Min((float)ringThickness, diameter / 2 - 2));
        return ((diameter - strokeWidth - 4) / 2, strokeWidth);
    }

    private static float GetSweep(double value)
    {
        return (float)(value * Math.PI * 2);
    }

    private static (float Start, float Sweep) TrimGap(float start, float sweep, float gap)
    {
        var effectiveGap = Math.Min(gap, sweep / 2);
        return (start + effectiveGap / 2, sweep - effectiveGap);
    }
}
