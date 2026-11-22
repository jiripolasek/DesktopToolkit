// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal enum LineChartSelectionPart
{
    Plot,
    Body,
    StartEdge,
    EndEdge,
}

internal readonly record struct LineChartSelectionEdit(LineChartSelectionPart Part, double Anchor, Rect Range)
{
    public const double DragThreshold = 4;

    public static LineChartSelectionPart HitTest(Rect? range, double x)
    {
        if (range is not { } bounds)
        {
            return LineChartSelectionPart.Plot;
        }

        var edge = Math.Min(8, bounds.Width / 3);
        if (Math.Abs(x - bounds.Left) <= edge)
        {
            return LineChartSelectionPart.StartEdge;
        }

        if (Math.Abs(x - bounds.Right) <= edge)
        {
            return LineChartSelectionPart.EndEdge;
        }

        return x >= bounds.Left && x <= bounds.Right ? LineChartSelectionPart.Body : LineChartSelectionPart.Plot;
    }

    public (double First, double Last) GetEdges(Rect plot, double x)
    {
        var delta = x - this.Anchor;
        if (this.Part == LineChartSelectionPart.Body)
        {
            var start = Math.Clamp(this.Range.Left + delta, plot.Left, Math.Max(plot.Left, plot.Right - this.Range.Width));
            return (start, start + this.Range.Width);
        }

        var minimumWidth = Math.Min(DragThreshold, this.Range.Width);
        return this.Part switch
        {
            LineChartSelectionPart.StartEdge => (Math.Clamp(this.Range.Left + delta, plot.Left, this.Range.Right - minimumWidth),
                this.Range.Right),
            LineChartSelectionPart.EndEdge => (this.Range.Left,
                Math.Clamp(this.Range.Right + delta, this.Range.Left + minimumWidth, plot.Right)),
            _ => (this.Anchor, Math.Clamp(x, plot.Left, plot.Right)),
        };
    }
}
