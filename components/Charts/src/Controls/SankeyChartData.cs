// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>One named endpoint in a Sankey diagram.</summary>
/// <param name="Key">The unique, ordinal identifier used by links.</param>
/// <param name="Label">The display label.</param>
/// <param name="Brush">An optional fill that overrides the chart palette.</param>
public sealed record SankeyChartNode(string Key, string Label, Brush? Brush = null);

/// <summary>A weighted flow from one Sankey node to another.</summary>
/// <param name="SourceKey">The key of the originating node.</param>
/// <param name="TargetKey">The key of the receiving node.</param>
/// <param name="Value">The finite, non-negative amount. Zero leaves the link out of the diagram.</param>
/// <param name="Brush">An optional fill that overrides the source node color.</param>
public sealed record SankeyChartLink(string SourceKey, string TargetKey, double Value, Brush? Brush = null);

/// <summary>Holds a directed, acyclic network of weighted flows.</summary>
public sealed class SankeyChartData
{
    /// <summary>Gets the nodes in input order.</summary>
    public IReadOnlyList<SankeyChartNode> Nodes { get; }

    /// <summary>Gets the links in input order, including links with a value of zero.</summary>
    public IReadOnlyList<SankeyChartLink> Links { get; }

    internal IndexedLink[] Edges { get; }

    internal int[][] Layers { get; }

    internal double[] IncomingTotals { get; }

    internal double[] OutgoingTotals { get; }

    /// <summary>Creates a snapshot of the nodes and links.</summary>
    /// <exception cref="ArgumentException">A node or link is invalid, or the positive links form a cycle.</exception>
    public SankeyChartData(SankeyChartNode[] nodes, SankeyChartLink[] links)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(links);
        var nodeSnapshot = (SankeyChartNode[])nodes.Clone();
        var linkSnapshot = (SankeyChartLink[])links.Clone();
        var indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < nodeSnapshot.Length; index++)
        {
            var node = nodeSnapshot[index];
            if (node is null
                || string.IsNullOrWhiteSpace(node.Key)
                || string.IsNullOrWhiteSpace(node.Label)
                || !indexes.TryAdd(node.Key, index))
            {
                throw new ArgumentException("Each node needs a unique key and a label.", nameof(nodes));
            }
        }

        var outgoing = new List<int>[nodeSnapshot.Length];
        var incomingCount = new int[nodeSnapshot.Length];
        this.IncomingTotals = new double[nodeSnapshot.Length];
        this.OutgoingTotals = new double[nodeSnapshot.Length];
        var edges = new List<IndexedLink>();
        foreach (var link in linkSnapshot)
        {
            if (link is null
                || link.SourceKey is null
                || link.TargetKey is null
                || !indexes.TryGetValue(link.SourceKey, out var source)
                || !indexes.TryGetValue(link.TargetKey, out var target)
                || !double.IsFinite(link.Value)
                || link.Value < 0)
            {
                throw new ArgumentException("Each link needs two known nodes and a finite, non-negative value.", nameof(links));
            }

            if (link.Value == 0)
            {
                continue;
            }

            this.OutgoingTotals[source] += link.Value;
            this.IncomingTotals[target] += link.Value;
            if (!double.IsFinite(this.OutgoingTotals[source]) || !double.IsFinite(this.IncomingTotals[target]))
            {
                throw new ArgumentException("The total flow entering or leaving a node must be finite.", nameof(links));
            }

            (outgoing[source] ??= []).Add(target);
            incomingCount[target]++;
            edges.Add(new(source, target, link));
        }

        var depth = GetDepths(outgoing, incomingCount);
        var last = depth.Length == 0 ? 0 : depth.Max();
        this.Layers = Enumerable.Range(0, nodeSnapshot.Length)
            .Where(index => this.IncomingTotals[index] > 0 || this.OutgoingTotals[index] > 0)
            .GroupBy(index => this.OutgoingTotals[index] > 0 ? depth[index] : last).OrderBy(layer => layer.Key)
            .Select(layer => layer.ToArray()).ToArray();
        this.Edges = edges.ToArray();
        this.Nodes = Array.AsReadOnly(nodeSnapshot);
        this.Links = Array.AsReadOnly(linkSnapshot);
    }

    private static int[] GetDepths(List<int>[] outgoing, int[] incomingCount)
    {
        var depth = new int[outgoing.Length];
        var ready = new Queue<int>(Enumerable.Range(0, outgoing.Length).Where(index => incomingCount[index] == 0));
        var visited = 0;
        while (ready.TryDequeue(out var source))
        {
            visited++;
            if (outgoing[source] is not { } targets)
            {
                continue;
            }

            foreach (var target in targets)
            {
                depth[target] = Math.Max(depth[target], depth[source] + 1);
                if (--incomingCount[target] == 0)
                {
                    ready.Enqueue(target);
                }
            }
        }

        if (visited != outgoing.Length)
        {
            throw new ArgumentException("Positive Sankey links must not form a cycle.", "links");
        }

        return depth;
    }

    internal readonly record struct IndexedLink(int Source, int Target, SankeyChartLink Link);
}
