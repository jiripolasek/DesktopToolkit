// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Windows.UI;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Associates a numeric value with a heatmap color.</summary>
public readonly record struct HeatmapColorStop(double Value, Color Color);

/// <summary>Maps finite measurements to continuous colors or discrete color bands.</summary>
public sealed class HeatmapColorScale
{
    private readonly HeatmapColorStop[] _stops;

    /// <summary>Gets the stops in ascending order.</summary>
    public IReadOnlyList<HeatmapColorStop> Stops { get; }

    /// <summary>Gets whether a stop defines a discrete band beginning at its value.</summary>
    public bool IsStepped { get; }

    /// <summary>Copies stops in ascending order. In stepped mode each stop starts a band.</summary>
    public HeatmapColorScale(HeatmapColorStop[] stops, bool isStepped = false)
    {
        ArgumentNullException.ThrowIfNull(stops);
        if (stops.Length == 0)
        {
            throw new ArgumentException("Supply at least one color stop.", nameof(stops));
        }

        this._stops = (HeatmapColorStop[])stops.Clone();
        for (var index = 0; index < this._stops.Length; index++)
        {
            if (!double.IsFinite(this._stops[index].Value) || (index > 0 && this._stops[index].Value <= this._stops[index - 1].Value))
            {
                throw new ArgumentException("Stop values must be finite and strictly increasing.", nameof(stops));
            }
        }

        this.Stops = Array.AsReadOnly(this._stops);
        this.IsStepped = isStepped;
    }

    /// <summary>Pairs an ordered color set with the same number of ascending stop values.</summary>
    /// <remarks>Pass the chart's ActualTheme and recreate the scale when its theme changes.</remarks>
    public static HeatmapColorScale FromPalette(
        ChartColorSet colors,
        double[] values,
        ElementTheme theme = ElementTheme.Default,
        bool isStepped = false)
    {
        ArgumentNullException.ThrowIfNull(colors);
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length != colors.Count)
        {
            throw new ArgumentException("Supply one stop value per palette color.", nameof(values));
        }

        var stops = new HeatmapColorStop[values.Length];
        for (var index = 0; index < stops.Length; index++)
        {
            stops[index] = new HeatmapColorStop(values[index], colors.GetColor(index, theme));
        }

        return new HeatmapColorScale(stops, isStepped);
    }

    /// <summary>Gets the color for a measurement, clamped to the available stops.</summary>
    public Color GetColor(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        if (value <= this._stops[0].Value)
        {
            return this._stops[0].Color;
        }

        var low = 0;
        var high = this._stops.Length - 1;
        while (high - low > 1)
        {
            var mid = low + (high - low) / 2;
            if (this._stops[mid].Value <= value)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        var left = this._stops[low];
        var right = this._stops[high];
        return value >= right.Value ? right.Color :
            this.IsStepped ? left.Color : Interpolate(left.Color, right.Color, ChartScale.Normalize(value, left.Value, right.Value));
    }

    internal static Color Interpolate(Color low, Color high, double fraction)
    {
        return Color.FromArgb(
            Mix(low.A, high.A, fraction), Mix(low.R, high.R, fraction), Mix(low.G, high.G, fraction), Mix(low.B, high.B, fraction));
    }

    private static byte Mix(byte low, byte high, double fraction)
    {
        return (byte)Math.Round(low + (high - low) * fraction);
    }
}
