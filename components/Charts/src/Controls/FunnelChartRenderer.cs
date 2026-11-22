// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal static class FunnelChartRenderer
{
    public static void Draw(FunnelChart owner, FlowChartPresenter presenter, Rect bounds)
    {
        if (owner.Data is not { Stages.Count: > 0 } data)
        {
            return;
        }

        if (owner.Presentation == FunnelChartPresentation.Flow)
        {
            FunnelFlowRenderer.Draw(owner, presenter, data, bounds);
            return;
        }

        var stages = data.Stages;
        var slot = (bounds.Height - 8) / stages.Count;
        var spacing = Math.Clamp(double.IsFinite(owner.StageSpacing) ? owner.StageSpacing : 6, 0, slot / 2);
        var height = (bounds.Height - 8 - spacing * (stages.Count - 1)) / stages.Count;
        var showLabels = owner.ShowLabels && bounds.Width >= 160 && height >= owner.FontSize * 1.4;
        var labelWidth = showLabels ? Math.Min(148, bounds.Width * 0.25) : 0;
        var valueWidth = showLabels ? Math.Min(104, bounds.Width * 0.20) : 0;
        var left = bounds.Left + labelWidth + 6;
        var right = bounds.Right - valueWidth - 6;
        var center = (left + right) / 2;
        var maximum = stages.Max(stage => stage.Value);

        double Width(double value)
        {
            return maximum == 0 ? 0 : Math.Max(0, right - left) * (value / maximum);
        }

        var radius = double.IsFinite(owner.StageCornerRadius) ? Math.Max(0, owner.StageCornerRadius) : 0;

        (double Start, double End) Span(double width)
        {
            return owner.StageAlignment switch
            {
                HorizontalAlignment.Left => (left, left + width),
                HorizontalAlignment.Right => (right - width, right),
                _ => (center - width / 2, center + width / 2),
            };
        }

        for (var index = 0; index < stages.Count; index++)
        {
            var stage = stages[index];
            var top = bounds.Top + 4 + index * (height + spacing);
            var bottom = top + height;
            var middle = (top + bottom) / 2;
            var width = Width(stage.Value);
            var nextWidth = owner.IsTapered && index + 1 < stages.Count ? Math.Min(width, Width(stages[index + 1].Value)) : width;
            var (start, end) = Span(width);
            var (nextStart, nextEnd) = Span(nextWidth);
            var geometry = CreateBand(
                new Point(start, top), new Point(end, top), new Point(nextStart, bottom), new Point(nextEnd, bottom), radius);
            var value = ChartInspectorPresenter.FormatValue(stage.Value, owner.ValueFormat);
            var fill = presenter.Fill(index, stage.Brush);
            presenter.AddMark(
                geometry, new Rect(bounds.Left, top, bounds.Width, height), fill,
                ReadStage(owner, data, index, new Point(end, middle), fill),
                new ChartContextValue(stage.Label, stage.Value, stage, CategoryIndex: index));
            if (showLabels)
            {
                presenter.AddLabel(stage.Label, bounds.Left + 2, middle, labelWidth - 4, TextAlignment.Left);
                presenter.AddLabel(value, right + 8, middle, valueWidth - 4, TextAlignment.Right);
            }
        }
    }

    private static PathGeometry CreateBand(Point topLeft, Point topRight, Point bottomLeft, Point bottomRight, double radius)
    {
        var halfHeight = (bottomLeft.Y - topLeft.Y) / 2;
        var topRadius = Math.Min(radius, Math.Min(halfHeight, (topRight.X - topLeft.X) / 2));
        var bottomRadius = Math.Min(radius, Math.Min(halfHeight, (bottomRight.X - bottomLeft.X) / 2));
        var topLeftInset = new Point(topLeft.X + topRadius, topLeft.Y);
        var topRightInset = new Point(topRight.X, topRight.Y + topRadius);
        var bottomRightInset = new Point(bottomRight.X, bottomRight.Y - bottomRadius);
        var bottomLeftInset = new Point(bottomLeft.X, bottomLeft.Y - bottomRadius);
        var figure = new PathFigure { StartPoint = topLeftInset, IsClosed = true };
        figure.Segments.Add(new LineSegment { Point = new Point(topRight.X - topRadius, topRight.Y) });
        AddCorner(topRight, topRightInset, topRadius);
        figure.Segments.Add(TaperSide(topRightInset, bottomRightInset));
        AddCorner(bottomRight, new Point(bottomRight.X - bottomRadius, bottomRight.Y), bottomRadius);
        figure.Segments.Add(new LineSegment { Point = new Point(bottomLeft.X + bottomRadius, bottomLeft.Y) });
        AddCorner(bottomLeft, bottomLeftInset, bottomRadius);
        figure.Segments.Add(TaperSide(bottomLeftInset, new Point(topLeft.X, topLeft.Y + topRadius)));
        AddCorner(topLeft, topLeftInset, topRadius);
        return new PathGeometry { Figures = { figure } };

        void AddCorner(Point vertex, Point end, double cornerRadius)
        {
            if (cornerRadius > 0)
            {
                figure.Segments.Add(new QuadraticBezierSegment { Point1 = vertex, Point2 = end });
            }
        }
    }

    private static PathSegment TaperSide(Point from, Point to)
    {
        if (from.X == to.X)
        {
            return new LineSegment { Point = to };
        }

        var third = (to.Y - from.Y) / 3;
        return new BezierSegment { Point1 = new Point(from.X, from.Y + third), Point2 = new Point(to.X, to.Y - third), Point3 = to, };
    }

    public static ChartInspectionItem ReadStage(
        FunnelChart owner,
        FunnelChartData data,
        int index,
        Point anchor,
        Brush? fill,
        IReadOnlyList<Brush>? seriesFills = null)
    {
        var stage = data.Stages[index];
        var value = ChartInspectorPresenter.FormatValue(stage.Value, owner.ValueFormat);
        var rows = new List<ChartInspectionRow> { new(WinUIResourceStrings.FormatDefault("LabelValueFormat", stage.Label, value), fill) };
        if (seriesFills is not null)
        {
            for (var series = 0; series < data.Series.Count; series++)
            {
                var cohort = data.Series[series];
                var cohortValue = ChartInspectorPresenter.FormatValue(cohort.Values[index], owner.ValueFormat);
                rows.Add(new(WinUIResourceStrings.FormatDefault("LabelValueFormat", cohort.Label, cohortValue), seriesFills[series]));
            }
        }

        if (index > 0)
        {
            AddPercentage(data.Stages[0].Value, "FunnelFirstStageFormat");
            AddPercentage(data.Stages[index - 1].Value, "FunnelPreviousStageFormat");
        }

        return new ChartInspectionItem(anchor, rows);

        void AddPercentage(double denominator, string formatKey)
        {
            var ratio = denominator > 0 ? stage.Value / denominator : double.NaN;
            if (double.IsFinite(ratio))
            {
                rows.Add(new(WinUIResourceStrings.FormatDefault(formatKey, ratio)));
            }
        }
    }
}
