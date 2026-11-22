// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Windows.UI;

namespace JPSoftworks.DesktopToolkit.Charts;

internal static class HierarchyChartPalette
{
    public static Color Surface(ElementTheme theme)
    {
        return theme == ElementTheme.Light ? Color.FromArgb(255, 250, 250, 250) : Color.FromArgb(255, 32, 32, 32);
    }

    public static Color Blend(Color surface, Color accent, double weight)
    {
        return Color.FromArgb(
            255, BlendChannel(surface.R, accent.R, weight), BlendChannel(surface.G, accent.G, weight),
            BlendChannel(surface.B, accent.B, weight));
    }

    private static byte BlendChannel(byte surface, byte accent, double weight)
    {
        return (byte)Math.Round(surface + (accent - surface) * weight);
    }
}
