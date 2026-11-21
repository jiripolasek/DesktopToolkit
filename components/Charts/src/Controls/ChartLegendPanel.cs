// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed partial class ChartLegendPanel : Panel
{
    private readonly List<Rect> _itemBounds = [];
    private Orientation _orientation;
    private double _spacing;

    public void SetLayout(Orientation orientation, double spacing)
    {
        if (this._orientation != orientation || this._spacing != spacing)
        {
            this._orientation = orientation;
            this._spacing = spacing;
            this.InvalidateMeasure();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        this._itemBounds.Clear();
        var width = availableSize.Width;
        var vertical = this._orientation == Orientation.Vertical;
        var x = 0d;
        var y = 0d;
        var rowHeight = 0d;
        var extent = 0d;
        foreach (var child in this.Children)
        {
            child.Measure(new Size(width, double.PositiveInfinity));
            var size = child.DesiredSize;
            var itemWidth = Math.Min(size.Width, width);
            if (x > 0 && (vertical || x + itemWidth > width))
            {
                x = 0;
                y += rowHeight + (vertical ? this._spacing : 4);
                rowHeight = 0;
            }

            this._itemBounds.Add(new Rect(x, y, itemWidth, size.Height));
            rowHeight = Math.Max(rowHeight, size.Height);
            extent = Math.Max(extent, x + itemWidth);
            x += itemWidth + this._spacing;
        }

        return new Size(extent, y + rowHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Preserve measured rows when the arranged width is rounded to device pixels.
        for (var index = 0; index < this._itemBounds.Count; index++)
        {
            this.Children[index].Arrange(this._itemBounds[index]);
        }

        return finalSize;
    }
}
