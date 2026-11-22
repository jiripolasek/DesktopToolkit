// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>One stage of a funnel.</summary>
/// <param name="Label">The display label.</param>
/// <param name="Value">The finite, non-negative measurement for this stage.</param>
/// <param name="Brush">An optional fill that overrides the chart palette.</param>
public sealed record FunnelChartStage(string Label, double Value, Brush? Brush = null);

/// <summary>Holds the ordered stages of a funnel chart.</summary>
public sealed class FunnelChartData
{
    /// <summary>Gets the stages in display order.</summary>
    public IReadOnlyList<FunnelChartStage> Stages { get; }

    /// <summary>Gets the cohorts in ribbon and legend order.</summary>
    public IReadOnlyList<FunnelChartSeries> Series { get; private init; } = [];

    /// <summary>Creates a snapshot of the stages in display order.</summary>
    /// <exception cref="ArgumentException">A stage is null or has an invalid label or value.</exception>
    public FunnelChartData(params FunnelChartStage[] stages)
    {
        ArgumentNullException.ThrowIfNull(stages);
        var snapshot = (FunnelChartStage[])stages.Clone();
        if (Array.Exists(
                snapshot,
                stage => stage is null || string.IsNullOrWhiteSpace(stage.Label) || !double.IsFinite(stage.Value) || stage.Value < 0))
        {
            throw new ArgumentException("Each stage needs a label and a finite, non-negative value.", nameof(stages));
        }

        this.Stages = Array.AsReadOnly(snapshot);
    }

    /// <summary>Creates a funnel whose stage totals are the sums of the supplied cohorts.</summary>
    /// <exception cref="ArgumentException">A cohort does not provide a value for every stage, or a total is not finite.</exception>
    public static FunnelChartData FromSeries(string[] stageLabels, params FunnelChartSeries[] series)
    {
        ArgumentNullException.ThrowIfNull(stageLabels);
        ArgumentNullException.ThrowIfNull(series);
        var snapshot = (FunnelChartSeries[])series.Clone();
        if (snapshot.Length == 0 || Array.Exists(snapshot, cohort => cohort is null || cohort.Values.Count != stageLabels.Length))
        {
            throw new ArgumentException("Supply at least one cohort with a value for every stage.", nameof(series));
        }

        var stages = new FunnelChartStage[stageLabels.Length];
        for (var index = 0; index < stages.Length; index++)
        {
            var total = 0d;
            foreach (var cohort in snapshot)
            {
                total += cohort.Values[index];
            }

            if (!double.IsFinite(total))
            {
                throw new ArgumentException("Each stage total must be finite.", nameof(series));
            }

            stages[index] = new FunnelChartStage(stageLabels[index], total);
        }

        return new FunnelChartData(stages) { Series = Array.AsReadOnly(snapshot) };
    }
}
