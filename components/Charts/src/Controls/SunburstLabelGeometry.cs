// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal static partial class SunburstLabelGeometry
{
    public static PathGeometry? Create(string text, SunburstSector sector, CanvasTextFormat format, bool mirrorCoordinates)
    {
        const double padding = 3;
        var height = sector.OuterRadius - sector.InnerRadius - 2 * padding;
        var radius = sector.MidRadius;
        var labelInnerRadius = sector.InnerRadius + padding;
        var width = radius * Math.Min(sector.VisibleSweep(labelInnerRadius) - 2 * padding / labelInnerRadius, Math.PI / 2);
        if (height < format.FontSize || width < 2.5 * format.FontSize || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var singleLine = text.Replace('\r', ' ').Replace('\n', ' ').Replace('\u2028', ' ').Replace('\u2029', ' ');
        using var layout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(), singleLine, format, (float)width, 2 * format.FontSize);
        using var outline = CanvasGeometry.CreateText(layout);
        var ink = outline.ComputeBounds();
        if (ink.Width <= 0 || ink.Width > width + 1 || ink.Height <= 0 || ink.Height > height)
        {
            return null;
        }

        using var flattened = outline.Simplify(CanvasGeometrySimplification.Lines, Matrix3x2.Identity, 0.12f);
        var radialDirection = Math.Sin(sector.MidAngle) > 0 ? -1 : 1;
        var angularDirection = mirrorCoordinates ? -radialDirection : radialDirection;
        var receiver = new ArcPathReceiver(sector, ink, radialDirection, angularDirection);
        flattened.SendPathTo(receiver);
        return receiver.Geometry;
    }

    private sealed partial class ArcPathReceiver(SunburstSector sector, Rect ink, int radialDirection, int angularDirection)
        : ICanvasPathReceiver
    {
        private Vector2 _start;
        private Vector2 _previous;
        private PathFigure _figure = null!;
        private PolyLineSegment _line = null!;

        public PathGeometry Geometry { get; } = new();

        public void BeginFigure(Vector2 startPoint, CanvasFigureFill figureFill)
        {
            this._start = this._previous = startPoint;
            this._figure = new PathFigure { StartPoint = this.Bend(startPoint), IsFilled = figureFill == CanvasFigureFill.Default };
            this._line = new PolyLineSegment();
            this._figure.Segments.Add(this._line);
        }

        public void AddLine(Vector2 endPoint)
        {
            // Subdivide straight glyph edges before bending them.
            var steps = Math.Max(1, (int)Math.Ceiling(Math.Abs(endPoint.X - this._previous.X) / 4));
            for (var step = 1; step <= steps; step++)
            {
                this._line.Points.Add(this.Bend(Vector2.Lerp(this._previous, endPoint, step / (float)steps)));
            }

            this._previous = endPoint;
        }

        public void EndFigure(CanvasFigureLoop figureLoop)
        {
            this._figure.IsClosed = figureLoop == CanvasFigureLoop.Closed;
            if (this._figure.IsClosed && this._previous != this._start)
            {
                this.AddLine(this._start);
            }

            this.Geometry.Figures.Add(this._figure);
        }

        public void SetFilledRegionDetermination(CanvasFilledRegionDetermination fill)
        {
            this.Geometry.FillRule = fill == CanvasFilledRegionDetermination.Winding ? FillRule.Nonzero : FillRule.EvenOdd;
        }

        public void SetSegmentOptions(CanvasFigureSegmentOptions options)
        {
        }

        public void AddArc(
            Vector2 endPoint,
            float radiusX,
            float radiusY,
            float rotationAngle,
            CanvasSweepDirection sweepDirection,
            CanvasArcSize arcSize)
        {
            throw new NotSupportedException();
        }

        public void AddCubicBezier(Vector2 controlPoint1, Vector2 controlPoint2, Vector2 endPoint)
        {
            throw new NotSupportedException();
        }

        public void AddQuadraticBezier(Vector2 controlPoint, Vector2 endPoint)
        {
            throw new NotSupportedException();
        }

        private Point Bend(Vector2 point)
        {
            var x = point.X - (ink.X + ink.Width / 2);
            var y = point.Y - (ink.Y + ink.Height / 2);
            return sector.At(sector.MidRadius - radialDirection * y, sector.MidAngle + angularDirection * x / sector.MidRadius);
        }
    }
}
