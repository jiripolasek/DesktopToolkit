// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal readonly record struct HeatmapLayout(double Left, double Top, double Width, double Height, double Gap)
{
    public bool IsValid => this.Width > 0 && this.Height > 0;

    public static HeatmapLayout Create(HeatmapChartData data, double width, double height, double left, double top, double gap, bool square)
    {
        var availableWidth = width - left;
        var availableHeight = height - top;
        var pitch = Math.Min(availableWidth / data.ColumnCount, availableHeight / data.RowCount);
        gap = double.IsFinite(gap) ? Math.Clamp(gap, 0, pitch / 2) : 0;
        var cellWidth = (availableWidth + gap) / data.ColumnCount - gap;
        var cellHeight = (availableHeight + gap) / data.RowCount - gap;
        if (square)
        {
            cellWidth = cellHeight = Math.Min(cellWidth, cellHeight);
            var plotWidth = (cellWidth + gap) * data.ColumnCount - gap;
            var plotHeight = (cellHeight + gap) * data.RowCount - gap;
            left += (availableWidth - plotWidth) / 2;
            top += (availableHeight - plotHeight) / 2;
        }

        return new HeatmapLayout(left, top, cellWidth, cellHeight, gap);
    }

    public Rect Cell(int row, int column)
    {
        return new(this.Left + column * (this.Width + this.Gap), this.Top + row * (this.Height + this.Gap), this.Width, this.Height);
    }

    public bool TryHit(Point point, HeatmapChartData data, out int row, out int column)
    {
        row = column = -1;
        if (!this.IsValid || point.X < this.Left || point.Y < this.Top)
        {
            return false;
        }

        row = (int)((point.Y - this.Top) / (this.Height + this.Gap));
        column = (int)((point.X - this.Left) / (this.Width + this.Gap));
        return row < data.RowCount && column < data.ColumnCount && this.Cell(row, column).Contains(point);
    }
}
