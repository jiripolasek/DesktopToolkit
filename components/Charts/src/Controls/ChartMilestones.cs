// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>An immutable snapshot of chart milestones.</summary>
public sealed class ChartMilestones
{
    /// <summary>Gets the milestones in the supplied order.</summary>
    public IReadOnlyList<ChartMilestone> Items { get; }

    /// <summary>Creates a snapshot by copying the supplied milestones.</summary>
    public ChartMilestones(params ChartMilestone[] items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var snapshot = (ChartMilestone[])items.Clone();
        if (Array.IndexOf(snapshot, null) >= 0)
        {
            throw new ArgumentException("Milestones cannot contain null entries.", nameof(items));
        }

        this.Items = Array.AsReadOnly(snapshot);
    }
}
