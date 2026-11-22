// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.UI;

namespace JPSoftworks.DesktopToolkit.Charts;

internal static class ChartColorGenerator
{
    private static readonly double HueStep = Math.PI * (3 - Math.Sqrt(5));

    public static ChartColorSet CreateCategorical(Color accent, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        var (hue, chroma) = ToHueChroma(accent);
        if (chroma < 0.01)
        {
            (hue, chroma) = ToHueChroma(ChartPalette.Default.GetColor(0));
        }

        chroma = Math.Clamp(chroma, 0.105, 0.18);
        var light = new Color[count];
        var dark = new Color[count];
        for (var index = 0; index < count; index++)
        {
            var angle = hue + index * HueStep;
            var variation = index % 2 == 0 ? 0 : 0.075;
            light[index] = FromOklch(0.56 + variation, chroma, angle);
            dark[index] = FromOklch(0.81 - variation, chroma, angle);
        }

        return new ChartColorSet(light, dark);
    }

    public static ChartColorSet CreateRamp(Color accent, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        var (hue, chroma) = ToHueChroma(accent);
        var light = new Color[count];
        var dark = new Color[count];
        for (var index = 0; index < count; index++)
        {
            var fraction = count == 1 ? 1 : (double)index / (count - 1);
            var saturation = Math.Min(0.18, chroma) * (0.25 + 0.75 * fraction);
            light[index] = FromOklch(0.94 - 0.46 * fraction, saturation, hue);
            dark[index] = FromOklch(0.30 + 0.56 * fraction, saturation, hue);
        }

        return new ChartColorSet(light, dark);
    }

    // Oklab conversion matrices from https://bottosson.github.io/posts/oklab/.
    private static (double Hue, double Chroma) ToHueChroma(Color color)
    {
        var r = ToLinear(color.R);
        var g = ToLinear(color.G);
        var blue = ToLinear(color.B);
        var l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * blue);
        var m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * blue);
        var s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * blue);
        var a = 1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s;
        var b = 0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s;
        return (Math.Atan2(b, a), Math.Sqrt(a * a + b * b));
    }

    private static Color FromOklch(double lightness, double chroma, double hue)
    {
        var (sine, cosine) = Math.SinCos(hue);
        var rgb = ToLinearRgb(lightness, chroma * cosine, chroma * sine);
        if (!IsInGamut(rgb))
        {
            // Reduce chroma to fit sRGB while retaining the hue and lightness.
            var low = 0d;
            var high = chroma;
            for (var iteration = 0; iteration < 12; iteration++)
            {
                var mid = (low + high) / 2;
                rgb = ToLinearRgb(lightness, mid * cosine, mid * sine);
                if (IsInGamut(rgb))
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            rgb = ToLinearRgb(lightness, low * cosine, low * sine);
        }

        return Color.FromArgb(255, ToSrgb(rgb.R), ToSrgb(rgb.G), ToSrgb(rgb.B));
    }

    private static (double R, double G, double B) ToLinearRgb(double lightness, double a, double b)
    {
        var l = Cube(lightness + 0.3963377774 * a + 0.2158037573 * b);
        var m = Cube(lightness - 0.1055613458 * a - 0.0638541728 * b);
        var s = Cube(lightness - 0.0894841775 * a - 1.2914855480 * b);
        return (4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s, -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
            -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s);
    }

    private static bool IsInGamut((double R, double G, double B) color)
    {
        return color.R is >= 0 and <= 1 && color.G is >= 0 and <= 1 && color.B is >= 0 and <= 1;
    }

    private static double Cube(double value)
    {
        return value * value * value;
    }

    private static double ToLinear(byte value)
    {
        var channel = value / 255d;
        return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }

    private static byte ToSrgb(double value)
    {
        var channel = value <= 0.0031308 ? value * 12.92 : 1.055 * Math.Pow(value, 1d / 2.4) - 0.055;
        return (byte)Math.Round(Math.Clamp(channel, 0, 1) * 255);
    }
}
