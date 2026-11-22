// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Windows.UI;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Provides predefined and generated color sets for charts.</summary>
public static class ChartPalette
{
    /// <summary>Identifies the Colors attached property.</summary>
    public static readonly DependencyProperty ColorsProperty = DependencyProperty.RegisterAttached(
        "Colors", typeof(ChartColorSet), typeof(ChartPalette), new PropertyMetadata(null));

    /// <summary>Gets the standard chart colors.</summary>
    public static ChartColorSet Default { get; } = Create(
        [0x0066B4, 0x8141B0, 0x00776F, 0xA94C17, 0xAD3168], [0x63AEF4, 0xC694F2, 0x52CBB8, 0xF6AC6C, 0xF184B1]);

    /// <summary>Gets saturated colors for distinguishing categories and series.</summary>
    public static ChartColorSet Vivid { get; } = Create(
        [0x005CC5, 0xC34813, 0x08795B, 0x8A3EC4, 0xBE3269, 0x866500], [0x67B0FF, 0xFFA363, 0x4FDBA5, 0xCF9DFF, 0xFF8BB7, 0xF0CA54]);

    /// <summary>Gets muted colors for compact dashboards.</summary>
    public static ChartColorSet Soft { get; } = Create(
        [0x4C6D94, 0x956079, 0x3D7F75, 0x966841, 0x726492, 0x637747], [0x9DB9D8, 0xCE9FB5, 0x8CBFB6, 0xCFB097, 0xB7A9D4, 0xACBC8D]);

    /// <summary>Gets colors inspired by forest, clay, water, and stone.</summary>
    public static ChartColorSet Earth { get; } = Create(
        [0x32664A, 0xAC553D, 0x2F6E91, 0x8E691D, 0x755568, 0x4D6683], [0x82BD9B, 0xF2A58D, 0x80BDE4, 0xDABC6C, 0xC99EB5, 0xA2B2C9]);

    /// <summary>Gets five ordered activity colors, starting with a neutral color for zero.</summary>
    public static ChartColorSet ActivityGreen { get; } = new(
        [Color.FromArgb(50, 120, 128, 136), Rgb(0xADE7C2), Rgb(0x6ECC98), Rgb(0x2B9C66), Rgb(0x176D47)],
        [Color.FromArgb(50, 120, 128, 136), Rgb(0x246B4E), Rgb(0x21955F), Rgb(0x3FBD78), Rgb(0x70DF99)]);

    /// <summary>Gets the override for a chart's automatic colors, or null to use Default.</summary>
    public static ChartColorSet? GetColors(DependencyObject chart)
    {
        return (ChartColorSet?)chart.GetValue(ColorsProperty);
    }

    /// <summary>Sets automatic colors for a bar, line, scatter, spider, treemap, icicle, or sunburst chart.</summary>
    public static void SetColors(DependencyObject chart, ChartColorSet? colors)
    {
        chart.SetValue(ColorsProperty, colors);
    }

    /// <summary>Generates opaque categorical colors using the accent hue as a starting point.</summary>
    /// <remarks>The accent's lightness is adapted to each theme. Neutral accents start at the standard blue hue.</remarks>
    public static ChartColorSet CreateFromAccent(Color accent, int count)
    {
        return ChartColorGenerator.CreateCategorical(accent, count);
    }

    /// <summary>Generates an ordered lightness ramp in the accent hue, from low to high intensity.</summary>
    public static ChartColorSet CreateRamp(Color accent, int count)
    {
        return ChartColorGenerator.CreateRamp(accent, count);
    }

    internal static ChartColorSet GetEffectiveColors(DependencyObject chart)
    {
        return GetColors(chart) ?? Default;
    }

    private static ChartColorSet Create(uint[] light, uint[] dark)
    {
        return new(Array.ConvertAll(light, Rgb), Array.ConvertAll(dark, Rgb));
    }

    private static Color Rgb(uint rgb)
    {
        return Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }
}
