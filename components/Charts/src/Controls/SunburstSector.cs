// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal readonly record struct SunburstSector(
    Point Center,
    double InnerRadius,
    double OuterRadius,
    double Start,
    double Sweep,
    double SideInset)
{
    public bool IsFullCircle => this.Sweep == Math.Tau;

    public double MidAngle => this.Start + this.Sweep / 2;

    public double MidRadius => (this.InnerRadius + this.OuterRadius) / 2;

    public Point At(double radius, double angle)
    {
        return new(this.Center.X + Math.Cos(angle) * radius, this.Center.Y + Math.Sin(angle) * radius);
    }

    public bool Contains(Point point)
    {
        var x = point.X - this.Center.X;
        var y = point.Y - this.Center.Y;
        var distanceSquared = x * x + y * y;
        return distanceSquared >= this.InnerRadius * this.InnerRadius
               && distanceSquared <= this.OuterRadius * this.OuterRadius
               && (this.IsFullCircle || this.ContainsAngle(Math.Atan2(y, x), this.AngularInset(Math.Sqrt(distanceSquared))));
    }

    public double VisibleSweep(double radius)
    {
        return this.Sweep - 2 * this.AngularInset(radius);
    }

    public Rect GetBounds()
    {
        if (this.IsFullCircle)
        {
            return new Rect(this.Center.X - this.OuterRadius, this.Center.Y - this.OuterRadius, 2 * this.OuterRadius, 2 * this.OuterRadius);
        }

        var outerInset = this.AngularInset(this.OuterRadius);
        var innerInset = this.AngularInset(this.InnerRadius);
        var end = this.Start + this.Sweep;
        var point = this.At(this.OuterRadius, this.Start + outerInset);
        var left = point.X;
        var right = point.X;
        var top = point.Y;
        var bottom = point.Y;
        Include(this.At(this.OuterRadius, end - outerInset));
        Include(this.At(this.InnerRadius, this.Start + innerInset));
        Include(this.At(this.InnerRadius, end - innerInset));
        for (var quarter = 0; quarter < 4; quarter++)
        {
            var angle = quarter * (Math.PI / 2);
            if (this.ContainsAngle(angle, outerInset))
            {
                Include(this.At(this.OuterRadius, angle));
            }
        }

        return new Rect(left, top, right - left, bottom - top);

        void Include(Point candidate)
        {
            left = Math.Min(left, candidate.X);
            right = Math.Max(right, candidate.X);
            top = Math.Min(top, candidate.Y);
            bottom = Math.Max(bottom, candidate.Y);
        }
    }

    public Geometry CreateGeometry()
    {
        if (this.IsFullCircle)
        {
            var outer = new EllipseGeometry { Center = this.Center, RadiusX = this.OuterRadius, RadiusY = this.OuterRadius };
            if (this.InnerRadius == 0)
            {
                return outer;
            }

            var inner = new EllipseGeometry { Center = this.Center, RadiusX = this.InnerRadius, RadiusY = this.InnerRadius };
            var ring = new GeometryGroup { FillRule = FillRule.EvenOdd };
            ring.Children.Add(outer);
            ring.Children.Add(inner);
            return ring;
        }

        var end = this.Start + this.Sweep;
        var outerInset = this.AngularInset(this.OuterRadius);
        var innerInset = this.AngularInset(this.InnerRadius);
        var figure = new PathFigure { StartPoint = this.At(this.OuterRadius, this.Start + outerInset), IsClosed = true, IsFilled = true };
        figure.Segments.Add(
            this.ArcTo(this.At(this.OuterRadius, end - outerInset), this.OuterRadius, SweepDirection.Clockwise, outerInset));
        figure.Segments.Add(new LineSegment { Point = this.At(this.InnerRadius, end - innerInset) });
        figure.Segments.Add(
            this.ArcTo(this.At(this.InnerRadius, this.Start + innerInset), this.InnerRadius, SweepDirection.Counterclockwise, innerInset));
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    // Offset each arc by the same perpendicular distance from the radial boundary.
    private double AngularInset(double radius)
    {
        return this.SideInset == 0 ? 0 : Math.Asin(this.SideInset / radius);
    }

    private bool ContainsAngle(double angle, double inset)
    {
        var offset = (angle - this.Start) % Math.Tau;
        offset = offset < 0 ? offset + Math.Tau : offset;
        return offset >= inset && offset <= this.Sweep - inset;
    }

    private ArcSegment ArcTo(Point point, double radius, SweepDirection direction, double inset)
    {
        return new()
        {
            Point = point, Size = new Size(radius, radius), SweepDirection = direction, IsLargeArc = this.Sweep - 2 * inset > Math.PI,
        };
    }
}
