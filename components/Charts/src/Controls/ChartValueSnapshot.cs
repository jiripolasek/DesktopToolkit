// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

internal static class ChartValueSnapshot
{
    public static double[] Copy(double[] values, int count)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length != count)
        {
            throw new ArgumentException("Supply one value per series.", nameof(values));
        }

        var result = (double[])values.Clone();
        if (Array.Exists(result, static value => !double.IsFinite(value) || value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(values), "Values must be finite and nonnegative.");
        }

        return result;
    }

    public static void ValidateNames<T>(T[] series, Func<T, string> name)
        where T : class
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in series)
        {
            if (item is null || string.IsNullOrWhiteSpace(name(item)) || !seen.Add(name(item)))
            {
                throw new ArgumentException("Supply a unique, nonempty name for every series.", nameof(series));
            }
        }
    }
}
