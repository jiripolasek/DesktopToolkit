// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>An immutable snapshot that defines the columns of a live graph sample.</summary>
public sealed class LiveGraphSeriesCollection
{
    /// <summary>Gets the series in sample-column order.</summary>
    public IReadOnlyList<LiveGraphSeries> Series { get; }

    internal LiveGraphSeries[] Snapshot { get; }

    /// <summary>Copies the supplied series in sample-column order.</summary>
    public LiveGraphSeriesCollection(params LiveGraphSeries[] series)
    {
        ArgumentNullException.ThrowIfNull(series);
        this.Snapshot = (LiveGraphSeries[])series.Clone();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in this.Snapshot)
        {
            if (item is null || !names.Add(item.Name))
            {
                throw new ArgumentException("Supply non-null series with unique names.", nameof(series));
            }
        }

        this.Series = Array.AsReadOnly(this.Snapshot);
    }
}
