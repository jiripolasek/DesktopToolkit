// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Gallery.UI;

public sealed partial class ResourceButtonPanel : Panel
{
    private const double Spacing = 6;

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in this.Children)
        {
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        }

        return this.Layout(availableSize.Width, arrange: false);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        this.Layout(finalSize.Width, arrange: true);
        return finalSize;
    }

    private Size Layout(double availableWidth, bool arrange)
    {
        var width = 0d;
        var rowWidth = 0d;
        var rowHeight = 0d;
        var y = 0d;
        foreach (var child in this.Children)
        {
            var size = child.DesiredSize;
            var x = rowWidth == 0 ? 0 : rowWidth + Spacing;
            if (x > 0 && x + size.Width > availableWidth)
            {
                y += rowHeight + Spacing;
                x = 0;
                rowHeight = 0;
            }

            if (arrange)
            {
                child.Arrange(new Rect(x, y, size.Width, size.Height));
            }

            rowWidth = x + size.Width;
            rowHeight = Math.Max(rowHeight, size.Height);
            width = Math.Max(width, rowWidth);
        }

        return new Size(width, y + rowHeight);
    }
}
