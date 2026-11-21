// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Controls;

internal sealed partial class NativeMenuItemsPanel : Panel
{
    private bool _bottom;
    private double _offset;
    private double _viewportLimit;

    internal bool UpdateViewport(bool bottom, double offset, double viewportLimit)
    {
        if (this._bottom == bottom && Math.Abs(this._offset - offset) < 0.25 && Math.Abs(this._viewportLimit - viewportLimit) < 0.25)
        {
            return false;
        }

        this._bottom = bottom;
        this._offset = offset;
        this._viewportLimit = viewportLimit;
        this.InvalidateArrange();
        return true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0, height = 0;
        foreach (var child in this.Children)
        {
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            width = Math.Max(width, child.DesiredSize.Width);
            height += child.DesiredSize.Height;
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var header = this.Children.OfType<MenuSearchItem>().FirstOrDefault(item => item.IsFilter);
        var headerHeight = header?.DesiredSize.Height ?? 0;
        var viewport = this._viewportLimit > 0 ? Math.Min(this._viewportLimit, finalSize.Height) : finalSize.Height;
        var offset = Math.Min(this._offset, finalSize.Height - viewport);
        var y = this._bottom ? 0 : headerHeight;
        foreach (var child in this.Children)
        {
            var height = child.DesiredSize.Height;
            if (ReferenceEquals(child, header))
            {
                child.Arrange(new Rect(0, offset + (this._bottom ? Math.Max(0, viewport - height) : 0), finalSize.Width, height));
            }
            else
            {
                child.Arrange(new Rect(0, y, finalSize.Width, height));
                y += height;
            }
        }

        return finalSize;
    }
}
