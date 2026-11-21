// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Windows.UI;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>An ordered set of colors with corresponding light and dark variants.</summary>
public sealed class ChartColorSet
{
    /// <summary>Gets the colors for a light background.</summary>
    public IReadOnlyList<Color> LightColors { get; }

    /// <summary>Gets the matching colors for a dark background.</summary>
    public IReadOnlyList<Color> DarkColors { get; }

    /// <summary>Gets the number of colors in each theme.</summary>
    public int Count => this.LightColors.Count;

    /// <summary>Copies a color set to use in both themes.</summary>
    public ChartColorSet(Color[] colors)
        : this(colors, colors)
    {
    }

    /// <summary>Copies paired theme variants. Both arrays must have the same nonzero length.</summary>
    public ChartColorSet(Color[] lightColors, Color[] darkColors)
    {
        ArgumentNullException.ThrowIfNull(lightColors);
        ArgumentNullException.ThrowIfNull(darkColors);
        if (lightColors.Length == 0 || darkColors.Length != lightColors.Length)
        {
            throw new ArgumentException("Supply the same nonzero number of colors for each theme.");
        }

        var light = Array.AsReadOnly((Color[])lightColors.Clone());
        this.LightColors = light;
        this.DarkColors = ReferenceEquals(lightColors, darkColors) ? light : Array.AsReadOnly((Color[])darkColors.Clone());
    }

    /// <summary>Gets the color at the specified index, cycling through the set as needed.</summary>
    /// <remarks>Pass the chart's ActualTheme. Default selects the light variant.</remarks>
    public Color GetColor(int index, ElementTheme theme = ElementTheme.Default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        return (theme == ElementTheme.Dark ? this.DarkColors : this.LightColors)[index % this.Count];
    }
}
