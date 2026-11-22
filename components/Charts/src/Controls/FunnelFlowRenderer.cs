// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal static class FunnelFlowRenderer
{
    public static void Draw(FunnelChart owner, FlowChartPresenter presenter, FunnelChartData data, Rect bounds)
    {
        var stages = data.Stages;
        var count = stages.Count;
        var slot = (bounds.Width - 8) / count;
        var labelSpace = owner.FontSize * 3.2 + 8;
        var showLabels = owner.ShowLabels && slot >= 34 && bounds.Height >= labelSpace + 24;
        var top = bounds.Top + 4;
        var bottom = bounds.Bottom - (showLabels ? labelSpace : 4);
        var max = stages.Max(stage => stage.Value);
        var middle = (top + bottom) / 2;
        var upper = new double[count];
        var lower = new double[count];
        var columns = new double[count];
        for (var index = 0; index < count; index++)
        {
            columns[index] = bounds.Left + 4 + (index + 0.5) * slot;
            lower[index] = middle - Scale(stages[index].Value) / 2;
        }

        var series = data.Series;
        var fills = new Brush[Math.Max(1, series.Count)];
        for (var index = 0; index < fills.Length; index++)
        {
            var cohort = series.Count > 0 ? series[index] : null;
            fills[index] = presenter.Fill(index, cohort is null ? stages[0].Brush : cohort.Brush);
            for (var stage = 0; stage < count; stage++)
            {
                upper[stage] = lower[stage];
                lower[stage] += Scale(cohort?.Values[stage] ?? stages[stage].Value);
            }

            presenter.AddSeriesShape(CreateRibbon(columns, upper, lower, Math.Min(slot * 0.28, 26)), fills[index], index);
        }

        for (var index = 0; index < count; index++)
        {
            var stage = stages[index];
            var x = columns[index];
            var stageHeight = Scale(stage.Value);
            var guideHeight = Math.Clamp(stageHeight + 8, 8, Math.Max(8, bottom - top));
            var guide = new RectangleGeometry { Rect = new Rect(x - 0.75, middle - guideHeight / 2, 1.5, guideHeight) };
            var hitArea = new Rect(x - slot / 2, top, slot, bounds.Bottom - top);
            var item = FunnelChartRenderer.ReadStage(owner, data, index, new Point(x, middle), null, fills);
            presenter.AddMark(
                guide, hitArea, owner.Foreground, item, new ChartContextValue(stage.Label, stage.Value, stage, CategoryIndex: index), 0.40);

            if (showLabels)
            {
                var width = slot - 4;
                presenter.AddLabel(stage.Label, x - width / 2, bottom + owner.FontSize * 0.9 + 3, width, TextAlignment.Center);
                presenter.AddLabel(
                    ChartInspectorPresenter.FormatValue(stage.Value, owner.ValueFormat), x - width / 2, bottom + owner.FontSize * 2.3 + 3,
                    width, TextAlignment.Center);
            }
        }

        double Scale(double value)
        {
            return max > 0 ? value / max * (bottom - top) : 0;
        }
    }

    private static PathGeometry CreateRibbon(double[] columns, double[] top, double[] bottom, double cap)
    {
        var figure = new PathFigure { StartPoint = new Point(columns[0] - cap, top[0]), IsClosed = true };
        figure.Segments.Add(new LineSegment { Point = new Point(columns[0], top[0]) });
        for (var index = 1; index < columns.Length; index++)
        {
            AddCurve(index - 1, index, top);
        }

        figure.Segments.Add(
            new PolyLineSegment
            {
                Points =
                {
                    new Point(columns[^1] + cap, top[^1]),
                    new Point(columns[^1] + cap, bottom[^1]),
                    new Point(columns[^1], bottom[^1]),
                },
            });
        for (var index = columns.Length - 1; index > 0; index--)
        {
            AddCurve(index, index - 1, bottom);
        }

        figure.Segments.Add(new LineSegment { Point = new Point(columns[0] - cap, bottom[0]) });
        return new PathGeometry { Figures = { figure } };

        void AddCurve(int from, int to, double[] y)
        {
            var third = (columns[to] - columns[from]) / 3;
            figure.Segments.Add(
                new BezierSegment
                {
                    Point1 = new Point(columns[from] + third, y[from]),
                    Point2 = new Point(columns[to] - third, y[to]),
                    Point3 = new Point(columns[to], y[to]),
                });
        }
    }
}
