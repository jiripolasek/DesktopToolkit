// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class ChartLegendHighlight
{
    private readonly List<(UIElement Visual, int Series)> _marks = [];
    private int _selected = -1;

    public void ClearMarks()
    {
        this._marks.Clear();
    }

    public void Add(UIElement visual, int series)
    {
        visual.Opacity = OpacityFor(series, this._selected);
        this._marks.Add((visual, series));
    }

    public void Select(int series)
    {
        this._selected = series;
        foreach (var (visual, index) in this._marks)
        {
            visual.Opacity = OpacityFor(index, series);
        }
    }

    public static double OpacityFor(int series, int selected)
    {
        return selected < 0 || selected == series ? 1 : 0.35;
    }
}
