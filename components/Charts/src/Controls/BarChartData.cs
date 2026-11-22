// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Holds a snapshot of the categories and series for a bar chart.</summary>
public sealed class BarChartData
{
    /// <summary>Gets category labels in display order. Labels can repeat.</summary>
    public IReadOnlyList<string> Categories { get; }

    /// <summary>Gets stable category keys in display order, or null if none were supplied.</summary>
    public IReadOnlyList<string>? CategoryKeys { get; }

    /// <summary>Gets the series in group and legend order.</summary>
    public IReadOnlyList<BarChartSeries> Series { get; }

    /// <summary>Creates a snapshot. Every series must have one value per category.</summary>
    public BarChartData(string[] categories, BarChartSeries[] series)
        : this(categories, series, null)
    {
    }

    /// <summary>Creates a snapshot with optional unique keys for tracking categories across updates.</summary>
    public BarChartData(string[] categories, BarChartSeries[] series, string[]? categoryKeys)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(series);

        var categorySnapshot = (string[])categories.Clone();
        var seriesSnapshot = (BarChartSeries[])series.Clone();
        if (Array.Exists(categorySnapshot, category => category is null))
        {
            throw new ArgumentException("Category labels cannot be null.", nameof(categories));
        }

        if (Array.Exists(seriesSnapshot, item => item is null || item.Values.Count != categorySnapshot.Length))
        {
            throw new ArgumentException("Every series must have one value per category.", nameof(series));
        }

        if (categoryKeys is not null)
        {
            var keys = (string[])categoryKeys.Clone();
            if (keys.Length != categorySnapshot.Length
                || Array.Exists(keys, string.IsNullOrWhiteSpace)
                || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
            {
                throw new ArgumentException("Supply a unique, nonempty key for every category.", nameof(categoryKeys));
            }

            this.CategoryKeys = Array.AsReadOnly(keys);
        }

        var seriesKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in seriesSnapshot)
        {
            if (item.Key is string key && (string.IsNullOrWhiteSpace(key) || !seriesKeys.Add(key)))
            {
                throw new ArgumentException("Series keys must be nonempty and unique.", nameof(series));
            }
        }

        this.Categories = Array.AsReadOnly(categorySnapshot);
        this.Series = Array.AsReadOnly(seriesSnapshot);
    }
}
