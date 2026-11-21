// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>A weighted item or branch in a hierarchy.</summary>
public sealed class HierarchyChartNode
{
    /// <summary>Gets the stable, case-sensitive identifier for the node.</summary>
    public string Key { get; }

    /// <summary>Gets the display label. Labels can repeat.</summary>
    public string Label { get; }

    /// <summary>Gets the supplied value or the sum of the immediate children's values.</summary>
    public double Value { get; }

    /// <summary>Gets whether this node represents a branch with pending children.</summary>
    public bool IsDeferred { get; }

    /// <summary>Gets the child snapshot in the supplied order.</summary>
    public IReadOnlyList<HierarchyChartNode> Children { get; }

    /// <summary>Creates a leaf with a finite, nonnegative value.</summary>
    public HierarchyChartNode(string key, string label, double value)
        : this(key, label, value, isDeferred: false)
    {
    }

    private HierarchyChartNode(string key, string label, double value, bool isDeferred)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "A node value must be finite and nonnegative.");
        }

        this.Key = key;
        this.Label = label;
        this.Value = value;
        this.IsDeferred = isDeferred;
        this.Children = Array.Empty<HierarchyChartNode>();
    }

    /// <summary>Creates a branch and takes a snapshot of its children. Their total must be finite.</summary>
    public HierarchyChartNode(string key, string label, params HierarchyChartNode[] children)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(children);

        var snapshot = (HierarchyChartNode[])children.Clone();
        var total = 0d;
        foreach (var child in snapshot)
        {
            if (child is null)
            {
                throw new ArgumentException("Children cannot be null.", nameof(children));
            }

            total += child.Value;
        }

        if (!double.IsFinite(total))
        {
            throw new ArgumentException("The total value of the children must be finite.", nameof(children));
        }

        this.Key = key;
        this.Label = label;
        this.Value = total;
        this.Children = Array.AsReadOnly(snapshot);
    }

    /// <summary>Creates a branch from a known total while its children are pending.</summary>
    public static HierarchyChartNode CreateDeferredBranch(string key, string label, double value)
    {
        return new(key, label, value, isDeferred: true);
    }
}
