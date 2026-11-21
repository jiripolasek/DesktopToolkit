// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>An immutable weighted hierarchy for use by hierarchy charts.</summary>
public sealed class HierarchyChartData
{
    private readonly Dictionary<string, NodeLocation> _nodes = new(StringComparer.Ordinal);

    /// <summary>Gets the root node and its descendants.</summary>
    public HierarchyChartNode Root { get; }

    /// <summary>Creates a hierarchy. Node keys must be unique within the entire tree.</summary>
    public HierarchyChartData(HierarchyChartNode root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var pending = new Stack<NodeLocation>();
        pending.Push(new NodeLocation(root, null, 0));
        while (pending.TryPop(out var location))
        {
            var node = location.Node;
            if (!this._nodes.TryAdd(node.Key, location))
            {
                throw new ArgumentException($"Duplicate hierarchy node key '{node.Key}'.", nameof(root));
            }

            for (var index = 0; index < node.Children.Count; index++)
            {
                pending.Push(new NodeLocation(node.Children[index], node.Key, index));
            }
        }

        this.Root = root;
    }

    /// <summary>Finds a node by its exact key. Returns null when the key is absent.</summary>
    public HierarchyChartNode? FindNode(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return this._nodes.TryGetValue(key, out var location) ? location.Node : null;
    }

    /// <summary>Returns a snapshot with the given node's children replaced and ancestor totals updated.</summary>
    public HierarchyChartData WithChildren(string key, params HierarchyChartNode[] children)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(children);
        if (!this._nodes.TryGetValue(key, out var location))
        {
            throw new ArgumentException($"Hierarchy node '{key}' was not found.", nameof(key));
        }

        var replacement = new HierarchyChartNode(location.Node.Key, location.Node.Label, children);
        while (location.ParentKey is string parentKey)
        {
            var parent = this._nodes[parentKey];
            var siblings = parent.Node.Children.ToArray();
            siblings[location.ChildIndex] = replacement;
            replacement = new HierarchyChartNode(parent.Node.Key, parent.Node.Label, siblings);
            location = parent;
        }

        return new HierarchyChartData(replacement);
    }

    private readonly record struct NodeLocation(HierarchyChartNode Node, string? ParentKey, int ChildIndex);
}
